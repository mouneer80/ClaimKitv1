// Store the current theme preference in localStorage
const saveThemePreference = (theme) => {
    localStorage.setItem('claimkit-ui-theme', theme);

    // Also set as a cookie for server-side access
    document.cookie = `claimkit-ui-theme=${theme}; path=/; max-age=31536000`;

    // Dispatch an event for other scripts to listen for
    document.dispatchEvent(new CustomEvent('themeChanged', {
        detail: { theme: theme }
    }));
};

// Get the stored theme preference, default to classic
const getThemePreference = () => {
    // Try localStorage first
    const localTheme = localStorage.getItem('claimkit-ui-theme');
    if (localTheme) return localTheme;

    // Try to get from cookie as fallback
    const cookies = document.cookie.split(';');
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].trim();
        if (cookie.startsWith('claimkit-ui-theme=')) {
            return cookie.substring('claimkit-ui-theme='.length);
        }
    }

    // Default to classic if no preference found
    return 'classic';
};

// Apply the theme to the document
const applyTheme = (theme) => {
    // Remove all theme classes first
    document.body.classList.remove('classic-ui', 'modern-ui', 'compact-ui');

    // Add the selected theme class
    document.body.classList.add(`${theme}-ui`);

    // Update active state of buttons
    document.querySelectorAll('.theme-switcher .btn-theme').forEach(button => {
        if (button.getAttribute('data-theme') === theme) {
            button.classList.add('active');
        } else {
            button.classList.remove('active');
        }
    });

    // Apply theme to any open modals
    document.querySelectorAll('.modal').forEach(modal => {
        modal.classList.remove('classic-ui', 'modern-ui', 'compact-ui');
        modal.classList.add(`${theme}-ui`);
    });

    // Save preference
    saveThemePreference(theme);

    console.log(`Theme switched to: ${theme}`);
};

// Initialize the theme switcher
const initThemeSwitcher = () => {
    const theme = getThemePreference();

    // Apply the saved theme
    applyTheme(theme);

    // Add event listeners to theme switcher buttons
    document.querySelectorAll('.theme-switcher .btn-theme').forEach(button => {
        button.addEventListener('click', () => {
            const newTheme = button.getAttribute('data-theme');
            applyTheme(newTheme);
        });
    });

    // Add keyboard shortcut for accessibility (Alt+T)
    document.addEventListener('keydown', (e) => {
        if (e.altKey && e.key === 't') {
            e.preventDefault();

            // Find current theme index
            const themes = ['classic', 'modern', 'compact'];
            const currentTheme = getThemePreference();
            const currentIndex = themes.indexOf(currentTheme);

            // Switch to next theme
            const nextIndex = (currentIndex + 1) % themes.length;
            applyTheme(themes[nextIndex]);

            // Show feedback
            showThemeSwitchFeedback(themes[nextIndex]);
        }
    });

    console.log('Theme switcher initialized');
};

// Show feedback when theme is switched via keyboard
function showThemeSwitchFeedback(theme) {
    // Create feedback element if it doesn't exist
    let feedback = document.getElementById('theme-switch-feedback');
    if (!feedback) {
        feedback = document.createElement('div');
        feedback.id = 'theme-switch-feedback';
        feedback.style.position = 'fixed';
        feedback.style.top = '20px';
        feedback.style.right = '20px';
        feedback.style.padding = '10px 15px';
        feedback.style.borderRadius = '5px';
        feedback.style.backgroundColor = '#333';
        feedback.style.color = '#fff';
        feedback.style.zIndex = '9999';
        feedback.style.opacity = '0';
        feedback.style.transition = 'opacity 0.3s ease';
        document.body.appendChild(feedback);
    }

    // Update content and show
    feedback.textContent = `Theme: ${theme.charAt(0).toUpperCase() + theme.slice(1)}`;
    feedback.style.opacity = '1';

    // Hide after 2 seconds
    setTimeout(() => {
        feedback.style.opacity = '0';
    }, 2000);
}

// Handle modal behavior based on theme
function applyThemeToModal(modal) {
    if (!modal) return;

    const currentTheme = getThemePreference();

    // Remove all theme classes
    modal.classList.remove('classic-ui', 'modern-ui', 'compact-ui');

    // Add current theme class
    modal.classList.add(`${currentTheme}-ui`);

    console.log(`Applied ${currentTheme} theme to modal: ${modal.id}`);
}

// Override showModal if not already overridden
if (typeof window.originalShowModal === 'undefined' && typeof window.showModal === 'function') {
    window.originalShowModal = window.showModal;

    window.showModal = function (modalId) {
        // Call original function
        window.originalShowModal(modalId);

        // Apply theme after a short delay
        setTimeout(() => {
            const modal = document.getElementById(`modal-${modalId}`);
            if (modal) {
                applyThemeToModal(modal);
            }
        }, 50);
    };
}

// Run initialization when DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
    initThemeSwitcher();

    // If page has workflow steps, set up a theme modifier for them
    const workflowSteps = document.querySelector('.workflow-steps');
    if (workflowSteps) {
        // Add steps listener to update their appearance based on theme
        updateWorkflowStepsForTheme(getThemePreference());
    }
});

// Helper function to update workflow step styles based on theme
function updateWorkflowStepsForTheme(theme) {
    const workflowSteps = document.querySelector('.workflow-steps');
    if (!workflowSteps) return;

    // Apply theme-specific styles to steps
    if (theme === 'modern') {
        // More rounded, blue accents
        workflowSteps.querySelectorAll('.step-number').forEach(step => {
            step.style.borderRadius = '50%';
            step.style.border = '2px solid #e9ecef';
        });

        workflowSteps.querySelectorAll('.step-completed .step-number').forEach(step => {
            step.style.backgroundColor = '#3182ce';
            step.style.borderColor = '#3182ce';
        });

        workflowSteps.querySelectorAll('.step-active .step-number').forEach(step => {
            step.style.backgroundColor = '#4299e1';
            step.style.borderColor = '#4299e1';
        });
    }
    else if (theme === 'compact') {
        // Smaller, teal accents
        workflowSteps.querySelectorAll('.step-number').forEach(step => {
            step.style.width = '25px';
            step.style.height = '25px';
            step.style.fontSize = '0.9rem';
        });

        workflowSteps.querySelectorAll('.step-completed .step-number').forEach(step => {
            step.style.backgroundColor = '#38b2ac';
            step.style.borderColor = '#38b2ac';
        });

        workflowSteps.querySelectorAll('.step-active .step-number').forEach(step => {
            step.style.backgroundColor = '#4fd1c5';
            step.style.borderColor = '#4fd1c5';
        });

        // Thinner connectors
        workflowSteps.querySelectorAll('.step-connector').forEach(connector => {
            connector.style.height = '1px';
        });
    }
    else {
        // Classic theme (reset to defaults)
        workflowSteps.querySelectorAll('.step-number').forEach(step => {
            step.style.borderRadius = '50%';
            step.style.width = '30px';
            step.style.height = '30px';
            step.style.fontSize = '';
            step.style.border = '';
        });

        workflowSteps.querySelectorAll('.step-completed .step-number').forEach(step => {
            step.style.backgroundColor = '#28a745';
            step.style.borderColor = '#28a745';
        });

        workflowSteps.querySelectorAll('.step-active .step-number').forEach(step => {
            step.style.backgroundColor = '#007bff';
            step.style.borderColor = '#007bff';
        });

        workflowSteps.querySelectorAll('.step-connector').forEach(connector => {
            connector.style.height = '2px';
        });
    }
}

// Add a listener for theme changes to update workflow steps
document.addEventListener('themeChanged', function (e) {
    updateWorkflowStepsForTheme(e.detail.theme);
});// ClaimKit UI Theme Switcher

// Store the current theme preference in localStorage
const saveThemePreference = (theme) => {
    localStorage.setItem('claimkit-ui-theme', theme);
};

// Get the stored theme preference, default to classic
const getThemePreference = () => {
    return localStorage.getItem('claimkit-ui-theme') || 'classic';
};

// Apply the theme to the document
const applyTheme = (theme) => {
    // Remove all theme classes first
    document.body.classList.remove('classic-ui', 'modern-ui', 'compact-ui');

    // Add the selected theme class
    document.body.classList.add(`${theme}-ui`);

    // Update active state of buttons
    document.querySelectorAll('.theme-switcher button').forEach(button => {
        if (button.getAttribute('data-theme') === theme) {
            button.classList.add('active');
        } else {
            button.classList.remove('active');
        }
    });

    // Save preference
    saveThemePreference(theme);

    console.log(`Theme switched to: ${theme}`);
};

// Initialize the theme switcher
const initThemeSwitcher = () => {
    const theme = getThemePreference();

    // Apply the saved theme
    applyTheme(theme);

    // Add event listeners to theme switcher buttons
    document.querySelectorAll('.theme-switcher button').forEach(button => {
        button.addEventListener('click', () => {
            const newTheme = button.getAttribute('data-theme');
            applyTheme(newTheme);
        });
    });
};

// Run initialization when DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
    initThemeSwitcher();
});

// Handle modal behavior based on theme
function showModal(modalId) {
    const originalShowModal = window.originalShowModal || window.showModal;

    // Call the original showModal function
    if (typeof originalShowModal === 'function') {
        originalShowModal(modalId);
    }

    // Apply theme-specific behavior
    const currentTheme = getThemePreference();
    const modal = document.querySelector(`.modal[id*="${modalId}"]`);

    if (modal) {
        modal.classList.add(`${currentTheme}-ui`);
    }
}

// Store original showModal if not already stored
if (typeof window.originalShowModal === 'undefined' && typeof window.showModal === 'function') {
    window.originalShowModal = window.showModal;
    window.showModal = showModal;
}