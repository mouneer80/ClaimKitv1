// ClaimKitv1 Modal System - Enhanced for Doctor Workflow

// Store modal state
const modalState = {
    activeModal: null,
    activeTab: null,
    selectedEnhancedNotes: [],
    selectedDiagnoses: []
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
    document.body.addEventListener('click', function (e) {
        if (e.target.closest('.category-header')) {
            const categoryItem = e.target.closest('.category-item');
            categoryItem.classList.toggle('collapsed');
        }
    });

    // Handle tab switching
    document.body.addEventListener('click', function (e) {
        const tab = e.target.closest('.modal-tab');
        if (tab) {
            const tabId = tab.getAttribute('data-tab');
            switchTab(tabId);
        }
    });

    // Handle selection of enhanced notes
    document.body.addEventListener('click', function (e) {
        const noteCheckbox = e.target.closest('.enhanced-note-checkbox');
        if (noteCheckbox) {
            toggleNoteSelection(noteCheckbox);
        }
    });

    // Handle selection of diagnoses
    document.body.addEventListener('click', function (e) {
        const diagnosisCheckbox = e.target.closest('.diagnosis-checkbox');
        if (diagnosisCheckbox) {
            toggleDiagnosisSelection(diagnosisCheckbox);
        }
    });

    // Handle approve button in enhanced notes modal
    document.body.addEventListener('click', function (e) {
        if (e.target.closest('#btnApproveEnhancedNotes')) {
            approveEnhancedNotes();
        }
    });

    // Handle approve button in claim diagnosis modal
    document.body.addEventListener('click', function (e) {
        if (e.target.closest('#btnApproveClaimDiagnoses')) {
            approveClaimDiagnoses();
        }
    });
});

// Initialize the modal system
function initModalSystem() {
    // Get all ASP.NET panels that should be modals
    const panelsToConvert = ['pnlReviewResults', 'pnlEnhancedNotes', 'pnlGeneratedClaim'];

    panelsToConvert.forEach(panelId => {
        // Create modal elements whether the panel exists/visible or not
        createModalFromPanel(panelId);
    });

    // Hide original panels from the DOM flow
    hideOriginalPanels();
}

// Create a modal from an ASP.NET panel ID
function createModalFromPanel(panelId) {
    // Get the title based on panel ID
    const modalTitles = {
        'pnlReviewResults': 'Clinical Note Review',
        'pnlEnhancedNotes': 'Enhanced Clinical Notes',
        'pnlGeneratedClaim': 'Insurance Claim Generation'
    };

    const title = modalTitles[panelId] || 'Medical Information';

    // Create modal container if it doesn't exist
    let modal = document.getElementById(`modal-${panelId}`);
    if (!modal) {
        modal = document.createElement('div');
        modal.id = `modal-${panelId}`;
        modal.className = 'modal';
        document.body.appendChild(modal);

        // Prepare footer buttons based on panel type
        let footerButtons = '<button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>';

        if (panelId === 'pnlReviewResults') {
            footerButtons = `
                <button type="button" class="modal-btn modal-btn-primary" onclick="window.enhanceReviewedNotes()">Enhance These Notes</button>
                <button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>
            `;
        } else if (panelId === 'pnlEnhancedNotes') {
            footerButtons = `
                <button type="button" id="btnApproveEnhancedNotes" class="modal-btn modal-btn-primary">Approve Selected Notes</button>
                <button type="button" class="modal-btn modal-btn-primary" onclick="window.generateClaimFromEnhanced()">Generate Insurance Claim</button>
                <button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>
            `;
        } else if (panelId === 'pnlGeneratedClaim') {
            footerButtons = `
                <button type="button" id="btnApproveClaimDiagnoses" class="modal-btn modal-btn-primary">Approve Selected Diagnoses</button>
                <button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>
            `;
        }

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
                    ${footerButtons}
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
    if (!modal) {
        console.error(`Modal not found: modal-${modalId}`);
        return;
    }

    // Get the original panel
    const originalPanel = document.getElementById(modalId);
    if (!originalPanel) {
        console.error(`Panel not found: ${modalId}`);
        return;
    }

    // Make sure the modal body exists
    let modalBody = document.getElementById(`modal-body-${modalId}`);
    if (!modalBody) {
        console.error(`Modal body not found: modal-body-${modalId}`);
        return;
    }

    try {
        // Check if the panel has content and is visible
        const isPanelVisible = originalPanel.style.display !== 'none' &&
            !originalPanel.hasAttribute('hidden') &&
            originalPanel.getAttribute('visible') !== 'false';

        // If panel exists but might not be visible, we'll still try to get its content
        if (!isPanelVisible) {
            const originalVisibility = originalPanel.style.display;
            originalPanel.style.display = 'block'; // Temporarily make it visible to access content

            // Clone the panel content
            const clonedContent = originalPanel.cloneNode(true);

            // Restore original visibility
            originalPanel.style.display = originalVisibility;

            // Process the cloned content
            clonedContent.style.display = 'block';
            if (clonedContent.hasAttribute('visible')) {
                clonedContent.setAttribute('visible', 'true');
            }

            // Clear previous content and add new
            modalBody.innerHTML = '';
            modalBody.innerHTML = clonedContent.innerHTML;
        } else {
            // If the panel is visible, handle normally
            const clonedContent = originalPanel.cloneNode(true);
            clonedContent.style.display = 'block';
            if (clonedContent.hasAttribute('visible')) {
                clonedContent.setAttribute('visible', 'true');
            }

            modalBody.innerHTML = '';
            modalBody.innerHTML = clonedContent.innerHTML;
        }

        // If this is the enhanced notes panel, add selection checkboxes
        if (modalId === 'pnlEnhancedNotes') {
            enhanceNotesWithSelectionOptions(modalBody);
        }

        // If this is the generated claim panel, add selection checkboxes for diagnoses
        if (modalId === 'pnlGeneratedClaim') {
            enhanceClaimWithSelectionOptions(modalBody);
        }

        // Show modal
        modal.classList.add('show');

        // Store active modal
        modalState.activeModal = modal;

        // Prevent page scrolling
        document.body.style.overflow = 'hidden';
    } catch (e) {
        console.error('Error showing modal:', e);

        // Create a fallback message in case of error
        modalBody.innerHTML = `
            <div class="error-message">
                <p>There was an issue displaying the content. Please try again or refresh the page.</p>
                <p>Error details: ${e.message}</p>
            </div>
        `;

        // Still show the modal so user sees the error
        modal.classList.add('show');
        modalState.activeModal = modal;
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

// Add selection checkboxes to enhanced notes
function enhanceNotesWithSelectionOptions(modalBody) {
    // Find the container with the enhanced notes
    const notesContainer = modalBody.querySelector('.result-content');
    if (!notesContainer) return;

    try {
        // Parse the notes content
        const notesContent = notesContainer.textContent;
        let notesObject;

        try {
            notesObject = JSON.parse(notesContent);
        } catch (e) {
            console.error('Failed to parse enhanced notes:', e);
            return;
        }

        // Clear the container
        notesContainer.innerHTML = '';

        // Create a new formatted display with checkboxes
        const formattedContainer = document.createElement('div');
        formattedContainer.className = 'enhanced-notes-container';

        // Add an explanation for the doctor
        const explanation = document.createElement('div');
        explanation.className = 'notes-explanation';
        explanation.innerHTML = `
            <p>Below are your enhanced clinical notes. Please review and select the ones you'd like to include in your final documentation.</p>
            <p>These notes have been optimized for clarity and medical accuracy while maintaining your clinical assessment.</p>
        `;
        formattedContainer.appendChild(explanation);

        // If notes is an array, display each note with a checkbox
        if (Array.isArray(notesObject)) {
            notesObject.forEach((note, index) => {
                const noteElement = createSelectableNoteElement(note, index);
                formattedContainer.appendChild(noteElement);
            });
        }
        // If notes is an object with sections
        else if (typeof notesObject === 'object') {
            Object.entries(notesObject).forEach(([section, content], index) => {
                const sectionElement = document.createElement('div');
                sectionElement.className = 'enhanced-note-section';

                const sectionTitle = document.createElement('h4');
                sectionTitle.textContent = formatSectionTitle(section);
                sectionElement.appendChild(sectionTitle);

                const noteElement = createSelectableNoteElement(content, index, section);
                sectionElement.appendChild(noteElement);

                formattedContainer.appendChild(sectionElement);
            });
        }
        // If it's something else, display as is with a checkbox
        else {
            const noteElement = createSelectableNoteElement(notesObject, 0);
            formattedContainer.appendChild(noteElement);
        }

        notesContainer.appendChild(formattedContainer);
    } catch (e) {
        console.error('Error enhancing notes with selection options:', e);
        // Restore original content if there's an error
        notesContainer.innerHTML = `<pre>${notesContent}</pre>`;
    }
}

// Create a selectable note element with checkbox
function createSelectableNoteElement(note, index, section = null) {
    const container = document.createElement('div');
    container.className = 'enhanced-note-item';

    let noteContent = '';
    let noteId = `note-${index}`;

    if (section) {
        noteId = `note-${section}-${index}`;
    }

    // Handle different types of note content
    if (typeof note === 'string') {
        noteContent = note;
    } else if (typeof note === 'object') {
        noteContent = JSON.stringify(note, null, 2);
    } else {
        noteContent = String(note);
    }

    container.innerHTML = `
        <div class="note-selection">
            <input type="checkbox" id="${noteId}" class="enhanced-note-checkbox" 
                   data-index="${index}" data-section="${section || ''}" checked>
            <label for="${noteId}">Include in final documentation</label>
        </div>
        <div class="note-content">
            <pre>${noteContent}</pre>
        </div>
    `;

    return container;
}

// Add selection checkboxes to generated claim diagnoses
function enhanceClaimWithSelectionOptions(modalBody) {
    // Find the container with the claim data
    const claimContainer = modalBody.querySelector('.result-content');
    if (!claimContainer) return;

    try {
        // Parse the claim content
        const claimContent = claimContainer.textContent;
        let claimObject;

        try {
            claimObject = JSON.parse(claimContent);
        } catch (e) {
            console.error('Failed to parse claim data:', e);
            return;
        }

        // Clear the container
        claimContainer.innerHTML = '';

        // Create a new formatted display with checkboxes
        const formattedContainer = document.createElement('div');
        formattedContainer.className = 'claim-container';

        // Add an explanation for the doctor
        const explanation = document.createElement('div');
        explanation.className = 'claim-explanation';
        explanation.innerHTML = `
            <p>Below are the recommended diagnoses for the insurance claim. Please review and select the appropriate diagnoses that match your clinical assessment.</p>
            <p>These diagnoses have been generated based on your notes and comply with insurance requirements.</p>
        `;
        formattedContainer.appendChild(explanation);

        // Extract diagnoses from the claim object
        let diagnoses = [];
        if (claimObject.diagnoses) {
            diagnoses = claimObject.diagnoses;
        } else if (claimObject.diagnosis) {
            diagnoses = Array.isArray(claimObject.diagnosis) ? claimObject.diagnosis : [claimObject.diagnosis];
        } else if (claimObject.recommended_diagnoses) {
            diagnoses = claimObject.recommended_diagnoses;
        } else {
            // Try to find diagnoses in nested objects
            for (const key in claimObject) {
                if (typeof claimObject[key] === 'object' && claimObject[key] !== null) {
                    if (claimObject[key].diagnoses) {
                        diagnoses = claimObject[key].diagnoses;
                        break;
                    } else if (claimObject[key].diagnosis) {
                        diagnoses = Array.isArray(claimObject[key].diagnosis) ? claimObject[key].diagnosis : [claimObject[key].diagnosis];
                        break;
                    }
                }
            }
        }

        // If we found diagnoses, display them with checkboxes
        if (diagnoses.length > 0) {
            const diagnosesContainer = document.createElement('div');
            diagnosesContainer.className = 'diagnoses-container';

            const diagnosesTitle = document.createElement('h4');
            diagnosesTitle.textContent = 'Recommended Diagnoses';
            diagnosesContainer.appendChild(diagnosesTitle);

            diagnoses.forEach((diagnosis, index) => {
                const diagnosisElement = createSelectableDiagnosisElement(diagnosis, index);
                diagnosesContainer.appendChild(diagnosisElement);
            });

            formattedContainer.appendChild(diagnosesContainer);
        }

        // Display the full claim data in a collapsible section
        const fullClaimContainer = document.createElement('div');
        fullClaimContainer.className = 'full-claim-container collapsed';

        const fullClaimHeader = document.createElement('div');
        fullClaimHeader.className = 'full-claim-header';
        fullClaimHeader.innerHTML = `
            <h4>Complete Claim Details</h4>
            <span class="expand-icon">▼</span>
        `;
        fullClaimHeader.addEventListener('click', () => {
            fullClaimContainer.classList.toggle('collapsed');
        });

        const fullClaimContent = document.createElement('div');
        fullClaimContent.className = 'full-claim-content';
        fullClaimContent.innerHTML = `<pre>${JSON.stringify(claimObject, null, 2)}</pre>`;

        fullClaimContainer.appendChild(fullClaimHeader);
        fullClaimContainer.appendChild(fullClaimContent);
        formattedContainer.appendChild(fullClaimContainer);

        claimContainer.appendChild(formattedContainer);
    } catch (e) {
        console.error('Error enhancing claim with selection options:', e);
        // Restore original content if there's an error
        claimContainer.innerHTML = `<pre>${claimContent}</pre>`;
    }
}

// Create a selectable diagnosis element with checkbox
function createSelectableDiagnosisElement(diagnosis, index) {
    const container = document.createElement('div');
    container.className = 'diagnosis-item';

    let diagnosisContent = '';
    let diagnosisCode = '';
    let diagnosisDescription = '';

    // Handle different formats of diagnosis data
    if (typeof diagnosis === 'string') {
        diagnosisContent = diagnosis;
    } else if (typeof diagnosis === 'object') {
        if (diagnosis.code) diagnosisCode = diagnosis.code;
        if (diagnosis.description) diagnosisDescription = diagnosis.description;
        if (!diagnosisCode && !diagnosisDescription) {
            diagnosisContent = JSON.stringify(diagnosis);
        }
    }

    const diagnosisId = `diagnosis-${index}`;

    if (diagnosisCode || diagnosisDescription) {
        container.innerHTML = `
            <div class="diagnosis-selection">
                <input type="checkbox" id="${diagnosisId}" class="diagnosis-checkbox" 
                       data-index="${index}" checked>
                <label for="${diagnosisId}">Include in claim</label>
            </div>
            <div class="diagnosis-content">
                ${diagnosisCode ? `<div class="diagnosis-code">${diagnosisCode}</div>` : ''}
                ${diagnosisDescription ? `<div class="diagnosis-description">${diagnosisDescription}</div>` : ''}
            </div>
        `;
    } else {
        container.innerHTML = `
            <div class="diagnosis-selection">
                <input type="checkbox" id="${diagnosisId}" class="diagnosis-checkbox" 
                       data-index="${index}" checked>
                <label for="${diagnosisId}">Include in claim</label>
            </div>
            <div class="diagnosis-content">
                <pre>${diagnosisContent}</pre>
            </div>
        `;
    }

    return container;
}

// Toggle selection of an enhanced note
function toggleNoteSelection(checkbox) {
    const index = checkbox.getAttribute('data-index');
    const section = checkbox.getAttribute('data-section');

    const noteKey = section ? `${section}-${index}` : index;

    if (checkbox.checked) {
        // Add to selected notes if not already included
        if (!modalState.selectedEnhancedNotes.includes(noteKey)) {
            modalState.selectedEnhancedNotes.push(noteKey);
        }
    } else {
        // Remove from selected notes
        const keyIndex = modalState.selectedEnhancedNotes.indexOf(noteKey);
        if (keyIndex !== -1) {
            modalState.selectedEnhancedNotes.splice(keyIndex, 1);
        }
    }
}

// Toggle selection of a diagnosis
function toggleDiagnosisSelection(checkbox) {
    const index = checkbox.getAttribute('data-index');

    if (checkbox.checked) {
        // Add to selected diagnoses if not already included
        if (!modalState.selectedDiagnoses.includes(index)) {
            modalState.selectedDiagnoses.push(index);
        }
    } else {
        // Remove from selected diagnoses
        const indexPos = modalState.selectedDiagnoses.indexOf(index);
        if (indexPos !== -1) {
            modalState.selectedDiagnoses.splice(indexPos, 1);
        }
    }
}

// Handle approval of enhanced notes
function approveEnhancedNotes() {
    // Here we would send the selected notes back to the server
    // For now, just log and close the modal
    console.log('Approved notes:', modalState.selectedEnhancedNotes);

    // Trigger server-side handling via hidden button click
    const approveButton = document.getElementById('btnServerApproveNotes');
    if (approveButton) {
        // Store selected notes in a hidden field
        const selectedNotesField = document.getElementById('hdnSelectedNotes');
        if (selectedNotesField) {
            selectedNotesField.value = JSON.stringify(modalState.selectedEnhancedNotes);
        }

        // Click the button to trigger server-side event
        approveButton.click();
    }

    closeCurrentModal();
}

// Handle approval of claim diagnoses
function approveClaimDiagnoses() {
    // Here we would send the selected diagnoses back to the server
    console.log('Approved diagnoses:', modalState.selectedDiagnoses);

    // Trigger server-side handling via hidden button click
    const approveButton = document.getElementById('btnServerApproveDiagnoses');
    if (approveButton) {
        // Store selected diagnoses in a hidden field
        const selectedDiagnosesField = document.getElementById('hdnSelectedDiagnoses');
        if (selectedDiagnosesField) {
            selectedDiagnosesField.value = JSON.stringify(modalState.selectedDiagnoses);
        }

        // Click the button to trigger server-side event
        approveButton.click();
    }

    closeCurrentModal();
}

// Format section titles for display
function formatSectionTitle(section) {
    // Convert snake_case or camelCase to Title Case
    return section
        .replace(/_/g, ' ')
        .replace(/([A-Z])/g, ' $1')
        .replace(/^./, str => str.toUpperCase())
        .trim();
}

// Format review results into a nicer display
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
                        <span class="category-title">${formatSectionTitle(stepKey)}</span>
                        <span class="expand-icon">▼</span>
                    </div>
                    <div class="category-content">
                `;

                // Add each section
                for (const section in stepData) {
                    const resultClass = stepData[section].result?.toLowerCase().replace(/\s+/g, '-') || '';

                    // Make the result more doctor-friendly
                    let resultText = stepData[section].result || '';
                    if (resultText.toLowerCase() === 'consistent') {
                        resultText = 'Consistent with standards';
                    } else if (resultText.toLowerCase() === 'inconsistent') {
                        resultText = 'Needs revision';
                    }

                    formattedHtml += `
                        <div class="section-item">
                            <div class="section-header">
                                <strong>${formatSectionTitle(section)}:</strong>
                                <span class="status-badge status-${resultClass}">${resultText}</span>
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

// Function to forcibly hide the loading indicator
window.forceHideLoadingIndicator = function () {
    // Direct DOM manipulation to hide the loading indicator
    var loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'none';
    }

    // Use a failsafe timer to ensure it stays hidden
    //setTimeout(function () {
    //    var loadingIndicator = document.getElementById('loadingIndicator');
    //    if (loadingIndicator) {
    //        loadingIndicator.style.display = 'none';
    //    }
    //}, 500);
};

// Call the server to enhance notes
window.enhanceReviewedNotes = function () {
    // Trigger the server-side enhance button
    const enhanceButton = document.getElementById('btnEnhanceNotes');
    if (enhanceButton) {
        enhanceButton.click();
    }
};

// Call the server to generate a claim from enhanced notes
window.generateClaimFromEnhanced = function () {
    // Trigger the server-side generate claim button
    const generateClaimButton = document.getElementById('btnGenerateClaimFromNotes');
    if (generateClaimButton) {
        generateClaimButton.click();
    }
};

// Expose the functions globally for ASP.NET buttons
window.showReviewResultsModal = function () {
    // Hide the loading indicator if it's still showing
    const loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'none';
    }
    //showModal('pnlReviewResults');

    // Force hide the loading indicator
    //window.forceHideLoadingIndicator();

    // Show the modal
    showModal('pnlReviewResults');

    // Add a backup timer to hide the indicator again after the modal is shown
    setTimeout(window.forceHideLoadingIndicator, 1000);
};

window.showEnhancedNotesModal = function () {
    // Reset selections when opening
    modalState.selectedEnhancedNotes = [];
    showModal('pnlEnhancedNotes');
};

window.showGeneratedClaimModal = function () {
    // Reset selections when opening
    modalState.selectedDiagnoses = [];
    showModal('pnlGeneratedClaim');
};
