// ClaimKit Modal Theme Support
// Add this to claimkit-modal.js or as a separate file

/**
 * Modal Theme Manager - Handles theme application to modals in ClaimKit
 * This enhancement allows modals to respond to theme changes without modifying the core functionality
 */

// Function to apply current theme to a modal
function applyThemeToModal(modal) {
    if (!modal) return;

    // Get current theme
    const currentTheme = localStorage.getItem('claimkit-ui-theme') || 'classic';

    // Remove any existing theme classes
    modal.classList.remove('classic-ui', 'modern-ui', 'compact-ui');

    // Add the current theme class
    modal.classList.add(`${currentTheme}-ui`);

    // Apply theme to modal content sections
    applyThemeToModalContent(modal, currentTheme);
}

// Apply theme to specific modal content sections
function applyThemeToModalContent(modal, theme) {
    // Apply theme to categories
    const categories = modal.querySelectorAll('.category-item');
    categories.forEach(category => {
        category.classList.remove('classic-ui', 'modern-ui', 'compact-ui');
        category.classList.add(`${theme}-ui`);
    });

    // Apply theme to enhanced notes sections
    const sectionPanels = modal.querySelectorAll('.section-panel');
    sectionPanels.forEach(panel => {
        panel.classList.remove('classic-ui', 'modern-ui', 'compact-ui');
        panel.classList.add(`${theme}-ui`);
    });

    // Apply theme to buttons
    const buttons = modal.querySelectorAll('.btn');
    buttons.forEach(button => {
        // Preserve btn-primary, btn-secondary classes
        const isPrimary = button.classList.contains('btn-primary');
        const isSecondary = button.classList.contains('btn-secondary');

        button.classList.remove('classic-ui', 'modern-ui', 'compact-ui');
        button.classList.add(`${theme}-ui`);

        // Make sure primary/secondary classes are preserved
        if (isPrimary && !button.classList.contains('btn-primary')) {
            button.classList.add('btn-primary');
        }
        if (isSecondary && !button.classList.contains('btn-secondary')) {
            button.classList.add('btn-secondary');
        }
    });
}

// Override existing modal functions to apply themes
if (typeof window.showModal === 'function' && typeof window.originalShowModal === 'undefined') {
    // Keep a reference to the original function
    window.originalShowModal = window.showModal;

    // Override with theme-aware version
    window.showModal = function (modalId) {
        // Call the original function
        window.originalShowModal(modalId);

        // Apply theme to the modal
        setTimeout(() => {
            const modal = document.querySelector(`#modal-${modalId}`);
            applyThemeToModal(modal);
        }, 50);
    };
}

// Also override modal-specific functions
const modalFunctions = ['showReviewResultsModal', 'showEnhancedNotesModal', 'showGeneratedClaimModal'];
modalFunctions.forEach(funcName => {
    if (typeof window[funcName] === 'function' && typeof window[`original${funcName}`] === 'undefined') {
        // Keep a reference to the original function
        window[`original${funcName}`] = window[funcName];

        // Override with theme-aware version
        window[funcName] = function () {
            // Call the original function
            window[`original${funcName}`].apply(this, arguments);

            // Apply theme to any modal that's now showing
            setTimeout(() => {
                document.querySelectorAll('.modal.show').forEach(modal => {
                    applyThemeToModal(modal);
                });
            }, 100);
        };
    }
});

// Watch for theme changes to update open modals
function updateActiveModalsTheme(newTheme) {
    document.querySelectorAll('.modal.show').forEach(modal => {
        applyThemeToModal(modal);
    });
}

// Add event listener for theme changes
document.addEventListener('themeChanged', function (e) {
    const newTheme = e.detail.theme;
    updateActiveModalsTheme(newTheme);
});

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Create a MutationObserver to watch for new modals
    const observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            if (mutation.addedNodes.length) {
                mutation.addedNodes.forEach(node => {
                    if (node.classList && node.classList.contains('modal')) {
                        applyThemeToModal(node);
                    }
                });
            }
        });
    });

    // Start observing the document body for added modals
    observer.observe(document.body, { childList: true, subtree: true });
});

// Debug helper function - call this in browser console to check theme application
window.checkModalThemes = function () {
    const modals = document.querySelectorAll('.modal');
    console.log(`Found ${modals.length} modals`);

    modals.forEach(modal => {
        const hasClassic = modal.classList.contains('classic-ui');
        const hasModern = modal.classList.contains('modern-ui');
        const hasCompact = modal.classList.contains('compact-ui');

        console.log(`Modal ${modal.id}: ${hasClassic ? 'classic-ui' : ''} ${hasModern ? 'modern-ui' : ''} ${hasCompact ? 'compact-ui' : ''}`);
    });

    // Return suggestions if issues found
    if (modals.length === 0) {
        return "No modals found. Try opening a modal first.";
    }

    let hasIssues = false;
    modals.forEach(modal => {
        if (!modal.classList.contains('classic-ui') &&
            !modal.classList.contains('modern-ui') &&
            !modal.classList.contains('compact-ui')) {
            hasIssues = true;
        }
    });

    if (hasIssues) {
        return "Some modals may be missing theme classes. Try refreshing the page or manually applying themes.";
    } else {
        return "All modals have theme classes correctly applied.";
    }
};

console.log("ClaimKit Modal Theme Support loaded");