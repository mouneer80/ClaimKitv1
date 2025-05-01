/**
 * ClaimKit Medical Documentation Assistant
 * Optimized JavaScript for modals, selections, and UI functionality
 */

// Main state object to track application state
const appState = {
    // Modal state
    modal: {
        activeModal: null,
        activeTab: null
    },
    // Selection state
    selection: {
        selectedSections: [],
        selectedNotes: [],
        selectedDiagnoses: []
    }
};

// Document ready handler
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

    // Handle selection of sections, notes, and diagnoses
    document.body.addEventListener('click', function (e) {
        // Handle section checkbox clicks
        const sectionCheckbox = e.target.closest('.section-checkbox');
        if (sectionCheckbox) {
            toggleSectionSelection(sectionCheckbox);
        }

        // Handle note checkbox clicks
        const noteCheckbox = e.target.closest('.enhanced-note-checkbox');
        if (noteCheckbox) {
            toggleNoteSelection(noteCheckbox);
        }

        // Handle diagnosis checkbox clicks
        const diagnosisCheckbox = e.target.closest('.diagnosis-checkbox');
        if (diagnosisCheckbox) {
            toggleDiagnosisSelection(diagnosisCheckbox);
        }
    });

    // Handle section header toggles (expand/collapse)
    document.body.addEventListener('click', function (e) {
        const sectionToggle = e.target.closest('.section-toggle');
        if (sectionToggle) {
            const sectionPanel = sectionToggle.closest('.section-panel');
            const content = sectionPanel.querySelector('.section-content');

            content.classList.toggle('collapsed');
            sectionToggle.textContent = content.classList.contains('collapsed') ? '▶' : '▼';
        }
    });

    // Handle approval buttons
    document.body.addEventListener('click', function (e) {
        // Enhanced notes approval
        if (e.target.closest('#btnApproveEnhancedNotes')) {
            approveEnhancedNotes();
        }

        // Claim diagnoses approval
        if (e.target.closest('#btnApproveClaimDiagnoses')) {
            approveClaimDiagnoses();
        }
    });

    // JSON toggle functionality
    const jsonToggleButtons = document.querySelectorAll('.expand-collapse');
    if (jsonToggleButtons) {
        jsonToggleButtons.forEach(button => {
            button.addEventListener('click', toggleJsonContent);
        });
    }

    // Restore any saved text when page loads
    var savedText = sessionStorage.getItem('finalNotesText');
    var finalNotesField = document.getElementById('txtFinalNotes');
    if (savedText && finalNotesField) {
        finalNotesField.value = savedText;
    }
});

// Initialize the modal system
function initModalSystem() {
    // Get all ASP.NET panels that should be modals
    const panelsToConvert = ['pnlReviewResults', 'pnlEnhancedNotes', 'pnlGeneratedClaim'];

    panelsToConvert.forEach(panelId => {
        // Create modal elements
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
                <button type="button" class="modal-btn modal-btn-primary" onclick="window.showEnhancedNotesModal()">Continue to Enhanced Notes</button>
                <button type="button" class="modal-btn modal-btn-secondary close-modal">Close</button>
            `;
        } else if (panelId === 'pnlEnhancedNotes') {
            footerButtons = `                
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
        appState.modal.activeModal = modal;

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
        appState.modal.activeModal = modal;
        document.body.style.overflow = 'hidden';
    }
}

// Close the currently open modal
function closeCurrentModal() {
    if (appState.modal.activeModal) {
        appState.modal.activeModal.classList.remove('show');
        appState.modal.activeModal = null;

        // Re-enable page scrolling
        document.body.style.overflow = '';
    }
}

// Switch between tabs in a modal
function switchTab(tabId) {
    if (!appState.modal.activeModal) return;

    // Update active tab
    appState.modal.activeTab = tabId;

    // Remove active class from all tabs
    appState.modal.activeModal.querySelectorAll('.modal-tab').forEach(tab => {
        tab.classList.remove('active');
    });

    // Add active class to selected tab
    const selectedTab = appState.modal.activeModal.querySelector(`[data-tab="${tabId}"]`);
    if (selectedTab) {
        selectedTab.classList.add('active');
    }

    // Hide all tab panes
    appState.modal.activeModal.querySelectorAll('.tab-pane').forEach(pane => {
        pane.classList.remove('active');
    });

    // Show selected tab pane
    const selectedPane = appState.modal.activeModal.querySelector(`#tab-pane-${tabId}`);
    if (selectedPane) {
        selectedPane.classList.add('active');
    }
}

// Enhanced notes section - supports both data-driven and JSON-based methods
function enhanceNotesWithSelectionOptions(modalBody) {
    try {
        // Method 1: Check for data container first (data-driven approach)
        const dataContainer = modalBody.querySelector('#enhancedNotesDataContainer');

        if (dataContainer && dataContainer.getAttribute('data-json')) {
            // Reset selected sections
            appState.selection.selectedSections = [];

            // Use the data-driven approach
            const jsonData = dataContainer.getAttribute('data-json');
            const enhancedNotes = JSON.parse(jsonData);
            console.log('Enhanced notes parsed successfully from data attribute');

            // Get the display container
            const displayContainer = modalBody.querySelector('#enhancedNotesDisplayContainer');
            if (!displayContainer) {
                console.error('Display container not found');
                return;
            }

            // Build the formatted HTML
            let html = buildSectionBasedEnhancedNotes(enhancedNotes);

            // Set the HTML
            displayContainer.innerHTML = html;

            // Add event listeners for sections
            addSectionEventListeners(displayContainer);

            console.log('Enhanced notes displayed with sections:', appState.selection.selectedSections);
            return;
        }

        // Method 2: Fall back to direct JSON parsing from result-content
        const notesContainer = modalBody.querySelector('.result-content');
        if (!notesContainer) {
            console.error('Enhanced notes container not found');
            return;
        }

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

        // Reset selected notes
        appState.selection.selectedNotes = [];

        // If notes is an array, display each note with a checkbox
        if (Array.isArray(notesObject)) {
            notesObject.forEach((note, index) => {
                const noteElement = createSelectableNoteElement(note, index);
                formattedContainer.appendChild(noteElement);

                // Add to selected notes by default
                const noteKey = index.toString();
                if (!appState.selection.selectedNotes.includes(noteKey)) {
                    appState.selection.selectedNotes.push(noteKey);
                }
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

                // Add to selected notes by default
                const noteKey = section ? `${section}-${index}` : index.toString();
                if (!appState.selection.selectedNotes.includes(noteKey)) {
                    appState.selection.selectedNotes.push(noteKey);
                }
            });
        }
        // If it's something else, display as is with a checkbox
        else {
            const noteElement = createSelectableNoteElement(notesObject, 0);
            formattedContainer.appendChild(noteElement);

            // Add to selected notes by default
            if (!appState.selection.selectedNotes.includes("0")) {
                appState.selection.selectedNotes.push("0");
            }
        }

        // Add selection controls
        const selectionControls = document.createElement('div');
        selectionControls.className = 'selection-controls';
        selectionControls.innerHTML = `
            <div class="selection-header">
                <h3>Select Notes to Include</h3>
            </div>
            <div class="selection-actions">
                <button type="button" class="btn btn-secondary btn-sm" onclick="selectAllNotes()">Select All</button>
                <button type="button" class="btn btn-secondary btn-sm" onclick="deselectAllNotes()">Deselect All</button>
                <button type="button" id="btnApproveEnhancedNotes" class="btn btn-primary">Approve Selected Notes</button>
            </div>
        `;
        formattedContainer.appendChild(selectionControls);

        notesContainer.appendChild(formattedContainer);

        console.log('Enhanced notes displayed with direct JSON parsing');
    } catch (e) {
        console.error('Error formatting enhanced notes:', e);
        modalBody.innerHTML += `<div class="error-message">Error: ${e.message}</div>`;
    }
}

// Build section-based enhanced notes HTML
function buildSectionBasedEnhancedNotes(enhancedNotes) {
    let html = `
        <div class="enhanced-notes-container">
            <div class="notes-explanation">
                <p>Your clinical notes have been reviewed and automatically enhanced for clarity and completeness.</p>
                <p>Please select the sections you'd like to include in your final documentation.</p>
            </div>
            <div class="sections-container">
    `;

    // Add sections
    if (enhancedNotes.sections) {
        Object.keys(enhancedNotes.sections).forEach(sectionKey => {
            const section = enhancedNotes.sections[sectionKey];
            const sectionTitle = section.title || formatSectionName(sectionKey);

            html += `
                <div class="section-panel" id="section-${sectionKey}">
                    <div class="section-header">
                        <div class="section-checkbox-container">
                            <input type="checkbox" id="section-checkbox-${sectionKey}" 
                                class="section-checkbox" data-section="${sectionKey}" checked>
                            <label for="section-checkbox-${sectionKey}">${sectionTitle}</label>
                        </div>
                        <span class="section-toggle">▼</span>
                    </div>
                    <div class="section-content">
            `;

            // Add fields if available
            if (section.fields) {
                html += '<div class="section-fields">';
                Object.entries(section.fields).forEach(([fieldName, fieldValue]) => {
                    html += `
                        <div class="field-item">
                            <span class="field-name">${formatFieldName(fieldName)}:</span>
                            <span class="field-value">${fieldValue}</span>
                        </div>
                    `;
                });
                html += '</div>';
            }

            // Add subsections if available
            if (section.subsections) {
                html += '<div class="subsections-container">';
                Object.entries(section.subsections).forEach(([subsectionKey, subsection]) => {
                    const subsectionTitle = subsection.title || formatFieldName(subsectionKey);

                    html += `<div class="subsection"><h4>${subsectionTitle}</h4>`;

                    // Add fields
                    if (subsection.fields) {
                        html += '<div class="fields-container">';
                        Object.entries(subsection.fields).forEach(([fieldName, fieldValue]) => {
                            html += `
                                <div class="field-item">
                                    <span class="field-name">${formatFieldName(fieldName)}:</span>
                                    <span class="field-value">${fieldValue}</span>
                                </div>
                            `;
                        });
                        html += '</div>';
                    }

                    // Add items list
                    if (subsection.items && Array.isArray(subsection.items)) {
                        html += '<ul class="items-list">';
                        subsection.items.forEach(item => {
                            if (typeof item === 'string') {
                                html += `<li>${item}</li>`;
                            } else if (typeof item === 'object' && item !== null) {
                                let itemText = item.name || '';
                                if (item.icd_10_cm_code) {
                                    itemText += ` <span class="code">(ICD-10: ${item.icd_10_cm_code})</span>`;
                                } else if (item.cpt_code) {
                                    itemText += ` <span class="code">(CPT: ${item.cpt_code})</span>`;
                                }
                                html += `<li>${itemText}</li>`;
                            }
                        });
                        html += '</ul>';
                    }

                    html += '</div>'; // Close subsection
                });
                html += '</div>'; // Close subsections-container
            }

            html += `
                    </div>
                </div>
            `;

            // Add to selected sections by default
            appState.selection.selectedSections.push(sectionKey);
        });
    } else {
        html += '<div class="error-message">No sections found in the enhanced notes.</div>';
    }

    html += `
            </div>
            <div class="selection-controls">
                <div class="selection-header">
                    <h3>Select Sections to Include</h3>
                </div>
                <div class="selection-actions">
                    <button type="button" class="btn btn-secondary btn-sm" onclick="selectAllSections()">Select All</button>
                    <button type="button" class="btn btn-secondary btn-sm" onclick="deselectAllSections()">Deselect All</button>
                    <button type="button" class="btn btn-primary" onclick="approveSelectedSections()">Approve Selected Sections</button>
                </div>
            </div>
        </div>
    `;

    return html;
}

// Add event listeners for section checkboxes and toggles
function addSectionEventListeners(container) {
    // Add event listeners for checkboxes
    container.querySelectorAll('.section-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const sectionId = this.getAttribute('data-section');
            const sectionPanel = document.getElementById(`section-${sectionId}`);

            if (this.checked) {
                if (!appState.selection.selectedSections.includes(sectionId)) {
                    appState.selection.selectedSections.push(sectionId);
                }
                if (sectionPanel) sectionPanel.classList.add('selected');
            } else {
                const index = appState.selection.selectedSections.indexOf(sectionId);
                if (index !== -1) {
                    appState.selection.selectedSections.splice(index, 1);
                }
                if (sectionPanel) sectionPanel.classList.remove('selected');
            }

            console.log('Selected sections:', appState.selection.selectedSections);
        });
    });

    // Add toggle functionality
    container.querySelectorAll('.section-toggle').forEach(toggle => {
        toggle.addEventListener('click', function () {
            const sectionPanel = this.closest('.section-panel');
            const content = sectionPanel.querySelector('.section-content');

            content.classList.toggle('collapsed');
            this.textContent = content.classList.contains('collapsed') ? '▶' : '▼';
        });
    });
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

        // Clear existing selections
        appState.selection.selectedDiagnoses = [];

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

                // Add to selected diagnoses by default
                appState.selection.selectedDiagnoses.push(index.toString());
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

// Toggle section selection
function toggleSectionSelection(checkbox) {
    const sectionId = checkbox.getAttribute('data-section');
    const sectionPanel = document.getElementById(`section-${sectionId}`);

    if (checkbox.checked) {
        if (!appState.selection.selectedSections.includes(sectionId)) {
            appState.selection.selectedSections.push(sectionId);
        }
        if (sectionPanel) sectionPanel.classList.add('selected');
    } else {
        const index = appState.selection.selectedSections.indexOf(sectionId);
        if (index !== -1) {
            appState.selection.selectedSections.splice(index, 1);
        }
        if (sectionPanel) sectionPanel.classList.remove('selected');
    }

    console.log('Selected sections:', appState.selection.selectedSections);
}

// Toggle note selection
function toggleNoteSelection(checkbox) {
    const noteId = checkbox.getAttribute('data-index');
    const section = checkbox.getAttribute('data-section');

    const noteKey = section ? `${section}-${noteId}` : noteId;

    if (checkbox.checked) {
        if (!appState.selection.selectedNotes.includes(noteKey)) {
            appState.selection.selectedNotes.push(noteKey);
        }
    } else {
        const index = appState.selection.selectedNotes.indexOf(noteKey);
        if (index !== -1) {
            appState.selection.selectedNotes.splice(index, 1);
        }
    }

    console.log('Selected notes:', appState.selection.selectedNotes);
}

// Toggle diagnosis selection
function toggleDiagnosisSelection(checkbox) {
    const index = checkbox.getAttribute('data-index');

    if (checkbox.checked) {
        if (!appState.selection.selectedDiagnoses.includes(index)) {
            appState.selection.selectedDiagnoses.push(index);
        }
    } else {
        const indexPos = appState.selection.selectedDiagnoses.indexOf(index);
        if (indexPos !== -1) {
            appState.selection.selectedDiagnoses.splice(indexPos, 1);
        }
    }

    console.log('Selected diagnoses:', appState.selection.selectedDiagnoses);
}

// Select all sections
function selectAllSections() {
    const checkboxes = document.querySelectorAll('.section-checkbox');
    appState.selection.selectedSections = [];

    checkboxes.forEach(checkbox => {
        checkbox.checked = true;
        const sectionId = checkbox.getAttribute('data-section');

        if (!appState.selection.selectedSections.includes(sectionId)) {
            appState.selection.selectedSections.push(sectionId);
        }

        const sectionPanel = document.getElementById(`section-${sectionId}`);
        if (sectionPanel) sectionPanel.classList.add('selected');
    });

    console.log('Selected all sections:', appState.selection.selectedSections);
}

// Deselect all sections
function deselectAllSections() {
    const checkboxes = document.querySelectorAll('.section-checkbox');

    checkboxes.forEach(checkbox => {
        checkbox.checked = false;
        const sectionId = checkbox.getAttribute('data-section');

        const sectionPanel = document.getElementById(`section-${sectionId}`);
        if (sectionPanel) sectionPanel.classList.remove('selected');
    });

    appState.selection.selectedSections = [];
    console.log('Deselected all sections');
}

// Select all notes
function selectAllNotes() {
    const checkboxes = document.querySelectorAll('.enhanced-note-checkbox');
    appState.selection.selectedNotes = [];

    checkboxes.forEach(checkbox => {
        checkbox.checked = true;
        const noteId = checkbox.getAttribute('data-index');
        const section = checkbox.getAttribute('data-section') || '';
        const noteKey = section ? `${section}-${noteId}` : noteId;

        if (!appState.selection.selectedNotes.includes(noteKey)) {
            appState.selection.selectedNotes.push(noteKey);
        }

        const noteElement = document.getElementById(`note-${section ? section + '-' : ''}${noteId}`);
        if (noteElement) noteElement.classList.add('selected');
    });

    console.log('Selected all notes:', appState.selection.selectedNotes);
}

// Deselect all notes
function deselectAllNotes() {
    const checkboxes = document.querySelectorAll('.enhanced-note-checkbox');

    checkboxes.forEach(checkbox => {
        checkbox.checked = false;
        const noteId = checkbox.getAttribute('data-index');
        const section = checkbox.getAttribute('data-section') || '';

        const noteElement = document.getElementById(`note-${section ? section + '-' : ''}${noteId}`);
        if (noteElement) noteElement.classList.remove('selected');
    });

    appState.selection.selectedNotes = [];
    console.log('Deselected all notes');
}

// Approve selected sections
function approveSelectedSections() {
    console.log('Approving sections:', appState.selection.selectedSections);

    if (appState.selection.selectedSections.length === 0) {
        alert('Please select at least one section to approve.');
        return;
    }

    // Get the hidden field for selected notes
    var hdnSelectedNotes = document.getElementById('hdnSelectedNotes');
    if (!hdnSelectedNotes) {
        console.error('Hidden field for selected notes not found');
        alert('Error: Could not store your selections. Please try again.');
        return;
    }

    // Store the selections in the hidden field
    hdnSelectedNotes.value = JSON.stringify(appState.selection.selectedSections);
    console.log('Stored in hidden field:', hdnSelectedNotes.value);

    // Show loading indicator
    var loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'block';
    }

    // Close the modal
    closeCurrentModal();

    // Trigger the server-side approval button
    var approveButton = document.getElementById('btnServerApproveNotes');
    if (approveButton) {
        // Small delay to ensure UI updates
        setTimeout(function () {
            console.log('Clicking approve button');
            approveButton.click();
        }, 100);
    } else {
        console.error('Approve button not found');
        alert('Error: Could not submit your selections. Please try again.');

        // Hide loading indicator if we can't proceed
        if (loadingIndicator) {
            loadingIndicator.style.display = 'none';
        }
    }
}