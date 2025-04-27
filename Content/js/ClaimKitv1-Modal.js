// ClaimKitv1 Modal System

// Store modal state
const modalState = {
    activeModal: null,
    activeTab: null
};

document.addEventListener('DOMContentLoaded', function () {
    // Initialize the modal system
    initModalSystem();

    // Add event listeners to close buttons
    document.querySelectorAll('.close-modal').forEach(btn => {
        btn.addEventListener('click', closeCurrentModal);
    });

    // Add event listener for ESC key to close modal
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeCurrentModal();
        }
    });

    // Add click event to close modal when clicking outside
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', function (e) {
            if (e.target === this) {
                closeCurrentModal();
            }
        });
    });

    // Handle category collapse/expand
    document.querySelectorAll('.category-header').forEach(header => {
        header.addEventListener('click', function () {
            const categoryItem = this.closest('.category-item');
            categoryItem.classList.toggle('collapsed');
        });
    });

    // Handle tab switching
    document.querySelectorAll('.modal-tab').forEach(tab => {
        tab.addEventListener('click', function () {
            const tabId = this.getAttribute('data-tab');
            switchTab(tabId);
        });
    });
});

// Initialize the modal system
function initModalSystem() {
    // Get all ASP.NET panels that should be modals
    const panelsToConvert = ['pnlReviewResults', 'pnlEnhancedNotes', 'pnlGeneratedClaim'];

    panelsToConvert.forEach(panelId => {
        const panel = document.getElementById(panelId);
        if (panel) {
            // Create modal elements
            createModalFromPanel(panel, panelId);
        }
    });

    // Hide original panels from the DOM flow
    hideOriginalPanels();
}

// Create a modal from an ASP.NET panel
function createModalFromPanel(panel, panelId) {
    // Get the title based on panel ID
    const modalTitles = {
        'pnlReviewResults': 'Review Results',
        'pnlEnhancedNotes': 'Enhanced Notes',
        'pnlGeneratedClaim': 'Generated Claim'
    };

    const title = modalTitles[panelId] || 'Modal';

    // Create modal container if it doesn't exist
    let modal = document.getElementById(`modal-${panelId}`);
    if (!modal) {
        modal = document.createElement('div');
        modal.id = `modal-${panelId}`;
        modal.className = 'modal';
        document.body.appendChild(modal);

        // Build modal structure
        modal.innerHTML = `
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title">${title}</h2>
                    <span class="close-modal">&times;</span>
                </div>
                <div class="modal-body" id="modal-body-${panelId}">
                    <!-- Panel content will be moved here -->
                </div>
                <div class="modal-footer">
                    <button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>
                </div>
            </div>
        `;

        // Set up event listeners for this modal
        modal.querySelector('.close-modal').addEventListener('click', closeCurrentModal);
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                closeCurrentModal();
            }
        });
    }
}

// Hide original panels
function hideOriginalPanels() {
    const panelsToHide = ['pnlReviewResults', 'pnlEnhancedNotes', 'pnlGeneratedClaim'];

    panelsToHide.forEach(panelId => {
        const panel = document.getElementById(panelId);
        if (panel) {
            // Hide but preserve for server-side processing
            panel.style.display = 'none';
            panel.setAttribute('data-converted-to-modal', 'true');
        }
    });
}

// Show a specific modal
function showModal(modalId) {
    // Close any open modal first
    closeCurrentModal();

    // Find the modal
    const modal = document.getElementById(`modal-${modalId}`);
    if (!modal) return;

    // Get the original panel
    const originalPanel = document.getElementById(modalId);
    if (!originalPanel) return;

    // Move the content from panel to modal
    const modalBody = document.getElementById(`modal-body-${modalId}`);
    if (modalBody) {
        // Clone the panel content and append to modal
        const clonedContent = originalPanel.cloneNode(true);

        // Remove any hidden attributes from the clone
        clonedContent.style.display = 'block';
        if (clonedContent.hasAttribute('visible')) {
            clonedContent.setAttribute('visible', 'true');
        }

        // Clear previous content and add new
        modalBody.innerHTML = '';

        // Extract the panel's inner HTML content instead of using the panel itself
        modalBody.innerHTML = clonedContent.innerHTML;

        // Show modal
        modal.classList.add('show');

        // Store active modal
        modalState.activeModal = modal;

        // Prevent page scrolling
        document.body.style.overflow = 'hidden';
    }
}

// Close the currently open modal
function closeCurrentModal() {
    if (modalState.activeModal) {
        modalState.activeModal.classList.remove('show');
        modalState.activeModal = null;

        // Re-enable page scrolling
        document.body.style.overflow = '';
    }
}

// Switch between tabs in a modal
function switchTab(tabId) {
    if (!modalState.activeModal) return;

    // Update active tab
    modalState.activeTab = tabId;

    // Remove active class from all tabs
    modalState.activeModal.querySelectorAll('.modal-tab').forEach(tab => {
        tab.classList.remove('active');
    });

    // Add active class to selected tab
    const selectedTab = modalState.activeModal.querySelector(`[data-tab="${tabId}"]`);
    if (selectedTab) {
        selectedTab.classList.add('active');
    }

    // Hide all tab panes
    modalState.activeModal.querySelectorAll('.tab-pane').forEach(pane => {
        pane.classList.remove('active');
    });

    // Show selected tab pane
    const selectedPane = modalState.activeModal.querySelector(`#tab-pane-${tabId}`);
    if (selectedPane) {
        selectedPane.classList.add('active');
    }
}

// Expose the functions globally for ASP.NET buttons
window.showReviewResultsModal = function () {
    showModal('pnlReviewResults');
};

window.showEnhancedNotesModal = function () {
    showModal('pnlEnhancedNotes');
};

window.showGeneratedClaimModal = function () {
    showModal('pnlGeneratedClaim');
};

// Function to format the review results into a nicer display
function formatReviewResults(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Process each category item
    container.querySelectorAll('.category-item').forEach(item => {
        const categoryText = item.innerText;

        // Parse the feedback JSON
        try {
            // Find JSON content within the category
            const jsonMatch = categoryText.match(/\{[\s\S]*\}/);
            if (jsonMatch) {
                const jsonStr = jsonMatch[0];
                const feedbackObj = JSON.parse(jsonStr);

                // Create formatted display
                const stepKey = Object.keys(feedbackObj)[0];
                const stepData = feedbackObj[stepKey];

                let formattedHtml = `
                    <div class="category-header">
                        <span class="category-title">${stepKey}</span>
                        <span class="expand-icon">▼</span>
                    </div>
                    <div class="category-content">
                `;

                // Add each section
                for (const section in stepData) {
                    const resultClass = stepData[section].result?.toLowerCase().replace(/\s+/g, '-') || '';
                    formattedHtml += `
                        <div class="section-item">
                            <div class="section-header">
                                <strong>${section}:</strong>
                                <span class="status-badge status-${resultClass}">${stepData[section].result}</span>
                            </div>
                            <div class="section-reasoning">
                                ${stepData[section].reasoning}
                            </div>
                        </div>
                    `;
                }

                formattedHtml += '</div>';
                item.innerHTML = formattedHtml;
            }
        } catch (error) {
            console.error('Error formatting review result:', error);
        }
    });
}