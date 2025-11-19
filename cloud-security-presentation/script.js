// Presentation Navigation
let currentSlide = 1;
const totalSlides = 12;

// DOM Elements
const slides = document.querySelectorAll('.slide');
const prevBtn = document.getElementById('prevBtn');
const nextBtn = document.getElementById('nextBtn');
const currentSlideEl = document.getElementById('currentSlide');
const progressFill = document.getElementById('progressFill');

// Initialize
function init() {
    updateSlide();
    updateProgress();
}

// Update slide display
function updateSlide() {
    slides.forEach((slide, index) => {
        if (index + 1 === currentSlide) {
            slide.classList.add('active');
        } else {
            slide.classList.remove('active');
        }
    });
    
    currentSlideEl.textContent = currentSlide;
    
    // Disable/enable buttons
    prevBtn.disabled = currentSlide === 1;
    nextBtn.disabled = currentSlide === totalSlides;
    
    if (currentSlide === 1) {
        prevBtn.style.opacity = '0.5';
        prevBtn.style.cursor = 'not-allowed';
    } else {
        prevBtn.style.opacity = '1';
        prevBtn.style.cursor = 'pointer';
    }
    
    if (currentSlide === totalSlides) {
        nextBtn.style.opacity = '0.5';
        nextBtn.style.cursor = 'not-allowed';
    } else {
        nextBtn.style.opacity = '1';
        nextBtn.style.cursor = 'pointer';
    }
}

// Update progress bar
function updateProgress() {
    const progress = (currentSlide / totalSlides) * 100;
    progressFill.style.width = `${progress}%`;
}

// Navigate to next slide
function nextSlide() {
    if (currentSlide < totalSlides) {
        currentSlide++;
        updateSlide();
        updateProgress();
    }
}

// Navigate to previous slide
function prevSlide() {
    if (currentSlide > 1) {
        currentSlide--;
        updateSlide();
        updateProgress();
    }
}

// Event Listeners
prevBtn.addEventListener('click', prevSlide);
nextBtn.addEventListener('click', nextSlide);

// Keyboard navigation
document.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowRight' || e.key === ' ') {
        e.preventDefault();
        nextSlide();
    } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        prevSlide();
    } else if (e.key === 'Home') {
        e.preventDefault();
        currentSlide = 1;
        updateSlide();
        updateProgress();
    } else if (e.key === 'End') {
        e.preventDefault();
        currentSlide = totalSlides;
        updateSlide();
        updateProgress();
    }
});

// Touch/swipe support for mobile
let touchStartX = 0;
let touchEndX = 0;

document.addEventListener('touchstart', (e) => {
    touchStartX = e.changedTouches[0].screenX;
});

document.addEventListener('touchend', (e) => {
    touchEndX = e.changedTouches[0].screenX;
    handleSwipe();
});

function handleSwipe() {
    const swipeThreshold = 50;
    const diff = touchStartX - touchEndX;
    
    if (Math.abs(diff) > swipeThreshold) {
        if (diff > 0) {
            // Swipe left - next slide
            nextSlide();
        } else {
            // Swipe right - previous slide
            prevSlide();
        }
    }
}

// Animate elements on slide change
function animateSlideElements() {
    const activeSlide = document.querySelector('.slide.active');
    const animatedElements = activeSlide.querySelectorAll('.agenda-item, .threat-card, .tool-card, .trend-card, .practice-item');
    
    animatedElements.forEach((el, index) => {
        el.style.animation = 'none';
        setTimeout(() => {
            el.style.animation = `fadeInUp 0.5s ease forwards ${index * 0.1}s`;
        }, 10);
    });
}

// Add fadeInUp animation
const style = document.createElement('style');
style.textContent = `
    @keyframes fadeInUp {
        from {
            opacity: 0;
            transform: translateY(30px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
`;
document.head.appendChild(style);

// Initialize presentation
init();

// Add click handlers for interactive elements
document.querySelectorAll('.agenda-item').forEach(item => {
    item.addEventListener('click', function() {
        const slideNumber = Array.from(document.querySelectorAll('.agenda-item')).indexOf(this) + 3;
        if (slideNumber <= totalSlides) {
            currentSlide = slideNumber;
            updateSlide();
            updateProgress();
        }
    });
});

// Fullscreen toggle (F key)
document.addEventListener('keydown', (e) => {
    if (e.key === 'f' || e.key === 'F') {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen();
        } else {
            document.exitFullscreen();
        }
    }
});

// Print mode (P key)
document.addEventListener('keydown', (e) => {
    if (e.key === 'p' || e.key === 'P') {
        e.preventDefault();
        window.print();
    }
});

// Add help overlay (H key)
let helpVisible = false;

document.addEventListener('keydown', (e) => {
    if (e.key === 'h' || e.key === 'H') {
        e.preventDefault();
        toggleHelp();
    } else if (e.key === 'Escape' && helpVisible) {
        toggleHelp();
    }
});

function toggleHelp() {
    let helpOverlay = document.getElementById('helpOverlay');
    
    if (!helpOverlay) {
        helpOverlay = document.createElement('div');
        helpOverlay.id = 'helpOverlay';
        helpOverlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.9);
            color: white;
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
            font-family: 'Inter', sans-serif;
        `;
        
        helpOverlay.innerHTML = `
            <div style="max-width: 600px; padding: 40px;">
                <h2 style="font-size: 36px; margin-bottom: 30px;">Keyboard Shortcuts</h2>
                <div style="display: grid; gap: 15px; font-size: 18px;">
                    <div><strong>→ / Space:</strong> Next slide</div>
                    <div><strong>←:</strong> Previous slide</div>
                    <div><strong>Home:</strong> First slide</div>
                    <div><strong>End:</strong> Last slide</div>
                    <div><strong>F:</strong> Toggle fullscreen</div>
                    <div><strong>P:</strong> Print presentation</div>
                    <div><strong>H:</strong> Show/hide this help</div>
                    <div><strong>Esc:</strong> Close help</div>
                </div>
                <p style="margin-top: 30px; opacity: 0.7;">Press H or Esc to close</p>
            </div>
        `;
        
        helpOverlay.addEventListener('click', toggleHelp);
        document.body.appendChild(helpOverlay);
    }
    
    if (helpVisible) {
        helpOverlay.style.display = 'none';
        helpVisible = false;
    } else {
        helpOverlay.style.display = 'flex';
        helpVisible = true;
    }
}

// Auto-hide navigation after inactivity
let inactivityTimer;
const navigation = document.querySelector('.navigation');

function resetInactivityTimer() {
    clearTimeout(inactivityTimer);
    navigation.style.opacity = '1';
    
    inactivityTimer = setTimeout(() => {
        navigation.style.opacity = '0.3';
    }, 3000);
}

document.addEventListener('mousemove', resetInactivityTimer);
document.addEventListener('keydown', resetInactivityTimer);

navigation.style.transition = 'opacity 0.3s ease';

// Initialize inactivity timer
resetInactivityTimer();

console.log('Cloud Security Presentation loaded successfully!');
console.log('Press H for keyboard shortcuts');
