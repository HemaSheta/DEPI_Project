using Depi_Project.Data.UnitOfWork;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace Depi_Project.Controllers.Customer
{
    public static class StripeWebhookHandler
    {
        public static async Task HandleWebhook(HttpContext context)
        {
            var json = await new StreamReader(context.Request.Body).ReadToEndAsync();

            var stripeSettings = context.RequestServices.GetRequiredService<IOptions<StripeSettings>>();
            var webhookSecret = stripeSettings.Value.WebhookSecret;

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    context.Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
            catch
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid Stripe signature");
                return;
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                var bookingService = context.RequestServices.GetRequiredService<IBookingService>();
                var unitOfWork = context.RequestServices.GetRequiredService<IUnitOfWork>();
                var userManager = context.RequestServices.GetService<UserManager<IdentityUser>>();

                // Read identity user id if provided
                string identityUserId = null;
                if (session.Metadata.TryGetValue("IdentityUserId", out var idFromMeta) && !string.IsNullOrWhiteSpace(idFromMeta))
                    identityUserId = idFromMeta;
                else
                {
                    var email = session.CustomerDetails?.Email;
                    if (!string.IsNullOrWhiteSpace(email) && userManager != null)
                    {
                        var user = await userManager.FindByEmailAsync(email);
                        if (user != null) identityUserId = user.Id;
                    }
                }

                if (string.IsNullOrWhiteSpace(identityUserId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Could not determine IdentityUserId for this payment.");
                    return;
                }

                // Collect item_i metadata entries
                var items = new List<(int RoomId, DateTime CheckIn, DateTime CheckOut, float PricePerNight)>();
                foreach (var kv in session.Metadata)
                {
                    if (kv.Key.StartsWith("item_"))
                    {
                        // format: RoomId|yyyy-MM-dd|yyyy-MM-dd|price
                        var parts = kv.Value.Split('|');
                        if (parts.Length != 4) continue;

                        if (!int.TryParse(parts[0], out var rid)) continue;
                        if (!DateTime.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ci)) continue;
                        if (!DateTime.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var co)) continue;
                        if (!float.TryParse(parts[3], out var p)) continue;

                        items.Add((rid, ci, co, p));
                    }
                }

                if (!items.Any())
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("No cart items found in metadata.");
                    return;
                }

                // Now run validations and create bookings in a single transaction
                using var tx = unitOfWork.BeginTransaction();
                try
                {
                    // 1) Validate there are no overlaps among items themselves
                    for (int i = 0; i < items.Count; i++)
                    {
                        for (int j = i + 1; j < items.Count; j++)
                        {
                            var a = items[i];
                            var b = items[j];
                            bool overlap = a.CheckIn < b.CheckOut && a.CheckOut > b.CheckIn;
                            if (overlap)
                            {
                                // don't create any booking
                                tx.Rollback();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Payment received but cart had overlapping items; bookings were not created.");
                                return;
                            }
                        }
                    }

                    // 2) Validate each item against existing bookings in DB
                    foreach (var it in items)
                    {
                        if (!bookingService.IsRoomAvailable(it.RoomId, it.CheckIn, it.CheckOut))
                        {
                            tx.Rollback();
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync($"Payment received but Room {it.RoomId} is no longer available for the dates.");
                            return;
                        }
                    }

                    // 3) Validate user existing bookings do not overlap any item
                    var userBookings = bookingService.GetAllBookings()
                                        .Where(b => b.IdentityUserId == identityUserId)
                                        .ToList();
                    foreach (var it in items)
                    {
                        foreach (var ub in userBookings)
                        {
                            bool overlap = it.CheckIn < ub.CheckOutTime && it.CheckOut > ub.CheckTime;
                            if (overlap)
                            {
                                tx.Rollback();
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync("Payment received but you already have bookings that overlap cart dates; bookings were not created.");
                                return;
                            }
                        }
                    }

                    // 4) All validations passed — create bookings
                    var createdBookingIds = new List<int>();
                    foreach (var it in items)
                    {
                        var booking = new Booking
                        {
                            RoomId = it.RoomId,
                            IdentityUserId = identityUserId,
                            CheckTime = it.CheckIn,
                            CheckOutTime = it.CheckOut,
                            TotalPrice = (it.PricePerNight * Math.Max(1, (it.CheckOut.Date - it.CheckIn.Date).Days)),
                            PaymentStatus = "Paid"
                        };

                        // Add booking using unit of work's repo directly (we're inside transaction)
                        unitOfWork.Bookings.Add(booking);
                        unitOfWork.Save(); // save to generate id if needed
                        createdBookingIds.Add(booking.BookingId);

                        // Do NOT set Room.Status globally — we keep date-based logic only.
                        // If you still want to flag current-day bookings, you'd set Status if today is within range.
                    }

                    tx.Commit();

                    // Optionally clear user's cart server-side: we don't have session here,
                    // webhook can't access the user's session reliably. The frontend should clear session after redirect.
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("Bookings recorded successfully");
                    return;
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Server error creating bookings: " + ex.Message);
                    return;
                }
            }

            // Unhandled event
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Unhandled event type");
        }
    }
}
