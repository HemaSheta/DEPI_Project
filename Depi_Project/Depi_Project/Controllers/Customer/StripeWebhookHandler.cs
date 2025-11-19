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

namespace Depi_Project.Controllers.Customer
{
    public static class StripeWebhookHandler
    {
        public static async Task HandleWebhook(HttpContext context)
        {
            var json = await new StreamReader(context.Request.Body).ReadToEndAsync();

            // Load Stripe settings (from appsettings.json)
            var stripeSettings = context.RequestServices.GetRequiredService<IOptions<StripeSettings>>();
            var webhookSecret = stripeSettings.Value.WebhookSecret;

            Event stripeEvent;
            try
            {
                // Verify Stripe signature
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

            // When payment is successful
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                // Resolve services
                var bookingService = context.RequestServices.GetRequiredService<IBookingService>();
                var unitOfWork = context.RequestServices.GetRequiredService<IUnitOfWork>();
                var userManager = context.RequestServices.GetService<UserManager<IdentityUser>>();

                // 1) Get roomId and price and dates from metadata
                if (!session.Metadata.TryGetValue("RoomId", out var roomIdStr)
                    || !session.Metadata.TryGetValue("CheckIn", out var checkInStr)
                    || !session.Metadata.TryGetValue("CheckOut", out var checkOutStr)
                    || !session.Metadata.TryGetValue("TotalPrice", out var totalPriceStr))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing required metadata (RoomId / CheckIn / CheckOut / TotalPrice).");
                    return;
                }

                if (!int.TryParse(roomIdStr, out var roomId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid RoomId metadata.");
                    return;
                }

                if (!float.TryParse(totalPriceStr, out var totalPrice))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid TotalPrice metadata.");
                    return;
                }

                // 2) Determine IdentityUserId
                string identityUserId = null;

                // Preferred: metadata contains IdentityUserId
                if (session.Metadata.TryGetValue("IdentityUserId", out var identityIdFromMeta) && !string.IsNullOrWhiteSpace(identityIdFromMeta))
                {
                    identityUserId = identityIdFromMeta;
                }
                else
                {
                    // Fallback: try to find user by email from Stripe session (if available)
                    var email = session.CustomerDetails?.Email;
                    if (!string.IsNullOrWhiteSpace(email) && userManager != null)
                    {
                        var user = await userManager.FindByEmailAsync(email);
                        if (user != null)
                        {
                            identityUserId = user.Id;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(identityUserId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Could not determine Identity user for this payment. Include IdentityUserId in metadata or ensure session.customer_email is present.");
                    return;
                }

                // 3) Build booking model
                DateTime checkIn, checkOut;
                try
                {
                    checkIn = DateTime.Parse(checkInStr);
                    checkOut = DateTime.Parse(checkOutStr);
                }
                catch (Exception)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid check-in/out dates in metadata.");
                    return;
                }

                var booking = new Booking
                {
                    RoomId = roomId,
                    IdentityUserId = identityUserId,
                    CheckTime = checkIn,
                    CheckOutTime = checkOut,
                    TotalPrice = totalPrice,
                    PaymentStatus = "Paid"
                };

                // 4) Create booking via service (business rules applied there)
                try
                {
                    var created = bookingService.CreateBooking(booking);
                    if (!created)
                    {
                        // Booking not created (room unavailable or user overlap)
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Payment received but booking was not created (availability check failed).");
                        return;
                    }

                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("Booking recorded");
                    return;
                }
                catch (Exception ex)
                {
                    // In production you would log this
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Server error creating booking: {ex.Message}");
                    return;
                }
            }

            // Unhandled event
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Unhandled event type");
        }
    }
}
