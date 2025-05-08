// ClaimKitv1 Modal System - Enhanced for Doctor Workflow
function formatInsuranceClaimData(claimData) {
    // Create main container
    const formattedContainer = document.createElement('div');
    formattedContainer.className = 'claim-container';

    // Add the page title
    const pageTitle = document.createElement('h2');
    pageTitle.className = 'page-title text-center mb-4';
    pageTitle.textContent = claimData.title;
    formattedContainer.appendChild(pageTitle);

    // Process each section
    for (const sectionKey in claimData.sections) {
        const section = claimData.sections[sectionKey];

        // Create section container
        const sectionElement = document.createElement('div');
        sectionElement.className = 'section-container mb-4 border rounded shadow-sm overflow-hidden';

        // Create section header with background style
        const sectionHeader = document.createElement('div');
        sectionHeader.className = `section-header p-2 ${section.style}`;

        // Map background styles to color classes
        const bgStyleMap = {
            'bg-primary': 'bg-blue-600 text-white',
            'bg-secondary': 'bg-gray-600 text-white',
            'bg-success': 'bg-green-500 text-white',
            'bg-danger': 'bg-red-500 text-white',
            'bg-warning': 'bg-yellow-500 text-black',
            'bg-info': 'bg-blue-400 text-white',
            'bg-light': 'bg-gray-200 text-black',
            'bg-dark': 'bg-gray-800 text-white'
        };

        // Apply the mapped style class
        if (bgStyleMap[section.style]) {
            sectionHeader.className = `section-header p-2 ${bgStyleMap[section.style]}`;
        }

        // Add section title
        const sectionTitle = document.createElement('h3');
        sectionTitle.className = 'section-title font-bold';
        sectionTitle.textContent = section.title;
        sectionHeader.appendChild(sectionTitle);
        sectionElement.appendChild(sectionHeader);

        // Content container
        const contentContainer = document.createElement('div');
        contentContainer.className = 'content-container p-3';

        // Process different section content types

        // 1. If section has fields (key-value pairs)
        if (section.fields) {
            const fieldsGrid = document.createElement('div');
            fieldsGrid.className = 'fields-grid grid grid-cols-2 gap-2';

            for (const key in section.fields) {
                const fieldRow = document.createElement('div');
                fieldRow.className = 'field-row col-span-1';

                const fieldLabel = document.createElement('span');
                fieldLabel.className = 'field-label font-semibold capitalize';
                fieldLabel.textContent = key.replace(/_/g, ' ') + ': ';

                const fieldValue = document.createElement('span');
                fieldValue.className = 'field-value text-gray-700';
                fieldValue.textContent = section.fields[key];

                fieldRow.appendChild(fieldLabel);
                fieldRow.appendChild(fieldValue);
                fieldsGrid.appendChild(fieldRow);
            }

            contentContainer.appendChild(fieldsGrid);
        }

        // 2. If section has simple content
        if (section.content) {
            const contentParagraph = document.createElement('p');
            contentParagraph.className = 'content-text text-gray-700';
            contentParagraph.textContent = section.content;
            contentContainer.appendChild(contentParagraph);
        }

        // 3. If section has list items
        if (section.list_items) {
            const listElement = document.createElement('ul');
            listElement.className = 'list-items list-disc pl-6 space-y-1';

            section.list_items.forEach(item => {
                const listItem = document.createElement('li');
                listItem.className = 'list-item text-gray-700';
                listItem.textContent = item;
                listElement.appendChild(listItem);
            });

            contentContainer.appendChild(listElement);
        }

        // 4. If section has diagnoses
        if (section.diagnoses) {
            const diagnosesTitle = document.createElement('div');
            diagnosesTitle.className = 'diagnoses-title text-sm mb-2 text-blue-600';
            diagnosesTitle.textContent = 'Please review and select the appropriate diagnoses for the insurance claim:';
            contentContainer.appendChild(diagnosesTitle);

            const diagnosesContainer = document.createElement('div');
            diagnosesContainer.className = 'diagnoses-container space-y-3';

            section.diagnoses.forEach((diagnosis, index) => {
                const diagnosisElement = document.createElement('div');
                diagnosisElement.className = 'diagnosis-item flex items-start space-x-2 p-2 bg-gray-100 rounded';

                // Create checkbox for selection
                const checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.id = `diagnosis-${index}`;
                checkbox.className = 'diagnosis-checkbox mt-1';

                const label = document.createElement('label');
                label.htmlFor = `diagnosis-${index}`;
                label.className = 'diagnosis-label flex-1 cursor-pointer';

                const typeDiv = document.createElement('div');
                typeDiv.className = 'diagnosis-type font-medium';
                typeDiv.textContent = diagnosis.type;

                const descriptionDiv = document.createElement('div');
                descriptionDiv.className = 'diagnosis-description text-gray-700';
                descriptionDiv.innerHTML = `${diagnosis.description} <span class="text-blue-600 font-mono">(${diagnosis.code})</span>`;

                label.appendChild(typeDiv);
                label.appendChild(descriptionDiv);

                diagnosisElement.appendChild(checkbox);
                diagnosisElement.appendChild(label);
                diagnosesContainer.appendChild(diagnosisElement);
            });

            contentContainer.appendChild(diagnosesContainer);
        }

        // 5. If section has tests
        if (section.tests) {
            const testsTitle = document.createElement('div');
            testsTitle.className = 'tests-title font-medium mb-2';
            testsTitle.textContent = 'Requested Tests:';
            contentContainer.appendChild(testsTitle);

            const testsContainer = document.createElement('div');
            testsContainer.className = 'tests-container space-y-2';

            section.tests.forEach(test => {
                const testElement = document.createElement('div');
                testElement.className = 'test-item bg-gray-100 p-2 rounded';

                const testName = document.createElement('span');
                testName.className = 'test-name font-medium';
                testName.textContent = test.name;

                const testCode = document.createElement('span');
                testCode.className = 'test-code text-sm text-gray-600 ml-2';
                testCode.textContent = `CPT: ${test.cpt_code}`;

                testElement.appendChild(testName);
                testElement.appendChild(testCode);
                testsContainer.appendChild(testElement);
            });

            contentContainer.appendChild(testsContainer);
        }

        // 6. If section has medications
        if (section.medications) {
            const medsTitle = document.createElement('div');
            medsTitle.className = 'medications-title font-medium mb-2';
            medsTitle.textContent = 'Medications:';
            contentContainer.appendChild(medsTitle);

            const medsContainer = document.createElement('div');
            medsContainer.className = 'medications-container space-y-3';

            section.medications.forEach(medication => {
                if (medication.name !== "Unavailable") {
                    const medElement = document.createElement('div');
                    medElement.className = 'medication-item bg-gray-100 p-2 rounded';

                    const medHeader = document.createElement('div');
                    medHeader.className = 'medication-header font-medium';
                    medHeader.textContent = `${medication.name} - ${medication.dosage}`;

                    const medDetails = document.createElement('div');
                    medDetails.className = 'medication-details text-sm text-gray-600';

                    let detailsText = medication.frequency;
                    if (medication.duration) detailsText += ` · Duration: ${medication.duration}`;
                    if (medication.ndc_code) detailsText += ` · NDC: ${medication.ndc_code}`;
                    medDetails.textContent = detailsText;

                    medElement.appendChild(medHeader);
                    medElement.appendChild(medDetails);

                    if (medication.description) {
                        const medDescription = document.createElement('div');
                        medDescription.className = 'medication-description text-sm text-gray-700 mt-1';
                        medDescription.textContent = medication.description;
                        medElement.appendChild(medDescription);
                    }

                    medsContainer.appendChild(medElement);
                }
            });

            contentContainer.appendChild(medsContainer);
        }

        sectionElement.appendChild(contentContainer);
        formattedContainer.appendChild(sectionElement);
    }

    // Add Complete Claim Details in a collapsible section
    const fullClaimContainer = document.createElement('div');
    fullClaimContainer.className = 'full-claim-container mt-10 border-t pt-4';

    const fullClaimButton = document.createElement('button');
    fullClaimButton.className = 'full-claim-button px-3 py-1 bg-gray-200 rounded text-sm';
    fullClaimButton.textContent = 'Show Complete Claim Details';
    fullClaimButton.setAttribute('data-expanded', 'false');

    const fullClaimContent = document.createElement('div');
    fullClaimContent.className = 'full-claim-content mt-2 p-3 bg-gray-100 rounded overflow-auto max-h-96 hidden';

    const fullClaimPre = document.createElement('pre');
    fullClaimPre.className = 'full-claim-pre text-xs text-gray-700';
    fullClaimPre.textContent = JSON.stringify(claimData, null, 2);

    fullClaimContent.appendChild(fullClaimPre);

    // Add click event to toggle visibility
    fullClaimButton.addEventListener('click', function () {
        const isExpanded = this.getAttribute('data-expanded') === 'true';
        if (isExpanded) {
            fullClaimContent.classList.add('hidden');
            this.textContent = 'Show Complete Claim Details';
            this.setAttribute('data-expanded', 'false');
        } else {
            fullClaimContent.classList.remove('hidden');
            this.textContent = 'Hide Complete Claim Details';
            this.setAttribute('data-expanded', 'true');
        }
    });

    fullClaimContainer.appendChild(fullClaimButton);
    fullClaimContainer.appendChild(fullClaimContent);
    formattedContainer.appendChild(fullClaimContainer);

    // Add action buttons
    const actionContainer = document.createElement('div');
    actionContainer.className = 'action-container mt-4 flex justify-end space-x-2';

    const cancelButton = document.createElement('button');
    cancelButton.className = 'cancel-button px-4 py-2 bg-gray-200 rounded';
    cancelButton.textContent = 'Cancel';

    const submitButton = document.createElement('button');
    submitButton.className = 'submit-button px-4 py-2 bg-blue-600 text-white rounded';
    submitButton.textContent = 'Submit Claim';

    actionContainer.appendChild(cancelButton);
    actionContainer.appendChild(submitButton);
    formattedContainer.appendChild(actionContainer);

    return formattedContainer;
}
// Store modal state
const modalState = {
    activeModal: null,
    activeTab: null,
    selectedEnhancedNotes: [],
    selectedDiagnoses: []
};
// Store selected sections
var selectedSections = [];

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
            toggleNoteSelectionByCheckbox(noteCheckbox);
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
        //<button type="button" class="modal-btn modal-btn-primary" onclick="window.enhanceReviewedNotes()">Enhance These Notes</button>
        //<button type="button" class="modal-btn modal-btn-primary" onclick="window.generateClaimFromEnhanced()">Generate Insurance Claim</button>
        //<button type="button" id="btnApproveEnhancedNotes" class="modal-btn modal-btn-primary">Approve Selected Notes</button>
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

function showModal(modalId) {
    // Close any open modal first
    closeCurrentModal();

    console.log(`Attempting to show modal for: ${modalId}`);

    // Find the modal
    const modal = document.getElementById(`modal-${modalId}`);
    if (!modal) {
        console.error(`Modal not found: modal-${modalId}`);
        return;
    }

    // Get the original panel with more robust selector approach
    let originalPanel = document.getElementById(modalId);

    // If not found directly, try to find it with client ID pattern
    if (!originalPanel) {
        // Try common ASP.NET naming patterns
        const possibleIds = [
            modalId,
            `ctl00_${modalId}`,
            `ctl00_ContentPlaceHolder1_${modalId}`,
            `MainContent_${modalId}`
        ];

        for (const id of possibleIds) {
            const element = document.getElementById(id);
            if (element) {
                console.log(`Found panel with ID: ${id}`);
                originalPanel = element;
                break;
            }
        }

        // If still not found, try to find by class name or other attributes
        if (!originalPanel) {
            const panelsByClass = document.querySelectorAll(`.result-panel`);
            for (const panel of panelsByClass) {
                if (panel.id.includes(modalId) ||
                    panel.getAttribute('data-panel-id') === modalId ||
                    panel.getAttribute('data-original-id') === modalId) {
                    console.log(`Found panel by class: ${panel.id}`);
                    originalPanel = panel;
                    break;
                }
            }
        }
    }

    // If still not found after all attempts, create a basic panel
    if (!originalPanel) {
        console.warn(`Panel not found: ${modalId}, creating a placeholder`);

        // Create a placeholder panel
        originalPanel = document.createElement('div');
        originalPanel.id = modalId;
        originalPanel.style.display = 'none';
        originalPanel.innerHTML = '<div class="result-content">Loading content...</div>';
        document.body.appendChild(originalPanel);
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

/**
 * Enhanced version of enhanceNotesWithSelectionOptions that handles any JSON structure
 * while maintaining compatibility with the existing modal system
 */
function enhanceNotesWithSelectionOptions(modalBody) {
    try {
        // Reset selected sections
        selectedSections = [];

        console.log("Starting enhanceNotesWithSelectionOptions...");

        // Find the containers using existing pattern
        let dataContainer = modalBody.querySelector('#enhancedNotesDataContainer');
        let displayContainer = modalBody.querySelector('#enhancedNotesDisplayContainer');
        let resultContent = modalBody.querySelector('.result-content');

        console.log("Found containers:", {
            dataContainer: !!dataContainer,
            displayContainer: !!displayContainer,
            resultContent: !!resultContent
        });

        // If containers don't exist, create them from the result-content
        if (!dataContainer || !displayContainer) {
            console.log("Creating missing containers...");

            if (resultContent) {
                // Get the content text
                const contentText = resultContent.textContent || '';
                console.log("Content text length:", contentText.length);

                // Try to extract JSON from the content
                let jsonData = "";
                try {
                    // See if the entire content is valid JSON
                    JSON.parse(contentText);
                    jsonData = contentText;
                    console.log("Full content appears to be valid JSON");
                } catch (e) {
                    // Try to find JSON object pattern
                    const objMatch = contentText.match(/(\{[\s\S]*\})/);
                    if (objMatch && objMatch[1]) {
                        try {
                            JSON.parse(objMatch[1]);
                            jsonData = objMatch[1];
                            console.log("Found JSON object in content");
                        } catch (e2) {
                            console.error("Extracted object not valid JSON:", e2);
                        }
                    }
                }

                // If we found valid JSON, replace the resultContent with our containers
                if (jsonData) {
                    console.log("Creating containers with extracted JSON");

                    // Clear existing content
                    resultContent.innerHTML = '';

                    // Create the necessary containers
                    dataContainer = document.createElement('div');
                    dataContainer.id = 'enhancedNotesDataContainer';
                    dataContainer.style.display = 'none';
                    dataContainer.setAttribute('data-json', jsonData);

                    displayContainer = document.createElement('div');
                    displayContainer.id = 'enhancedNotesDisplayContainer';

                    // Add them to the DOM
                    resultContent.appendChild(dataContainer);
                    resultContent.appendChild(displayContainer);

                    console.log("Created containers successfully");
                } else {
                    console.error("Could not extract valid JSON from content");
                    resultContent.innerHTML = '<div class="error-message">Unable to process notes data. Please try again.</div>';
                    return;
                }
            } else {
                console.error("No result content found to process");
                modalBody.innerHTML = '<div class="error-message">No content found to display. Please try again.</div>';
                return;
            }
        }

        // Get JSON from the data attribute
        const jsonData = dataContainer.getAttribute('data-json');
        if (!jsonData) {
            console.error('No JSON data found in container');
            displayContainer.innerHTML = '<div class="error-message">No data found to display. Please try again.</div>';
            return;
        }

        // Parse the JSON
        const enhancedNotes = JSON.parse(jsonData);
        console.log('Enhanced notes parsed successfully');

        // Build the formatted HTML for sections
        let html = `
            <div class="enhanced-notes-container">
                <div class="notes-explanation">
                    <p>Your clinical notes have been reviewed and automatically enhanced for clarity and completeness.</p>
                    <p>Please select the sections you'd like to include in your final documentation.</p>
                </div>
                <div class="sections-container">
        `;

        // Process the enhanced notes object based on structure
        if (enhancedNotes.sections) {
            // Process sections
            html += processEnhancedNoteSections(enhancedNotes.sections, selectedSections);
        } else if (Array.isArray(enhancedNotes)) {
            // If it's an array, process as array of sections
            html += processEnhancedNoteArray(enhancedNotes, selectedSections);
        } else {
            // If it's a generic object, handle it differently
            html += processGenericEnhancedNote(enhancedNotes, selectedSections);
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

        // Set the HTML
        displayContainer.innerHTML = html;

        // Add event listeners for section checkboxes
        addSectionEventListeners(displayContainer);

        console.log('Enhanced notes displayed with sections:', selectedSections);
    } catch (e) {
        console.error('Error formatting enhanced notes:', e);
        modalBody.innerHTML += `<div class="error-message">Error: ${e.message}</div>`;
    }
}

/**
 * Process sections from the enhanced notes
 */
function processEnhancedNoteSections(sections, selectedSectionsArray) {
    let html = '';

    // Process each section
    Object.keys(sections).forEach(sectionKey => {
        const section = sections[sectionKey];
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

        // Process the section based on its type and structure
        if (typeof section === 'object' && section !== null) {
            if (Array.isArray(section)) {
                // Section is an array
                html += processArraySection(section, sectionKey);
            } else {
                // Section is an object
                html += processObjectSection(section, sectionKey);
            }
        } else {
            // Simple value
            html += `<div class="section-simple-value">${section}</div>`;
        }

        html += `
                </div>
            </div>
        `;

        // Add to selected sections by default
        if (!selectedSectionsArray.includes(sectionKey)) {
            selectedSectionsArray.push(sectionKey);
        }
    });

    return html;
}

/**
 * Process array of enhanced notes
 */
function processEnhancedNoteArray(notesArray, selectedSectionsArray) {
    let html = '';

    // Process each item in the array as a section
    notesArray.forEach((item, index) => {
        const sectionKey = `item-${index}`;
        const sectionTitle = item.title || `Section ${index + 1}`;

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

        // Process based on type
        if (typeof item === 'object' && item !== null) {
            html += processObjectSection(item, sectionKey);
        } else {
            html += `<div class="section-simple-value">${item}</div>`;
        }

        html += `
                </div>
            </div>
        `;

        // Add to selected sections by default
        if (!selectedSectionsArray.includes(sectionKey)) {
            selectedSectionsArray.push(sectionKey);
        }
    });

    return html;
}

/**
 * Process a generic enhanced note object that doesn't follow standard structure
 */
function processGenericEnhancedNote(noteObject, selectedSectionsArray) {
    let html = '';

    // Process top-level properties as sections
    Object.keys(noteObject).forEach(key => {
        // Skip non-data properties
        if (key === 'title' || key === 'style') {
            return;
        }

        const value = noteObject[key];
        const sectionKey = key;
        const sectionTitle = formatSectionName(key);

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

        // Process based on type
        if (typeof value === 'object' && value !== null) {
            if (Array.isArray(value)) {
                html += processArraySection(value, sectionKey);
            } else {
                html += processObjectSection(value, sectionKey);
            }
        } else {
            html += `<div class="section-simple-value">${value}</div>`;
        }

        html += `
                </div>
            </div>
        `;

        // Add to selected sections by default
        if (!selectedSectionsArray.includes(sectionKey)) {
            selectedSectionsArray.push(sectionKey);
        }
    });

    return html;
}

/**
 * Process an object section
 */
function processObjectSection(section, sectionKey) {
    let html = '';

    // Process fields if available
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

    // Process subsections if available
    if (section.subsections) {
        html += '<div class="subsections-container">';
        Object.entries(section.subsections).forEach(([subsectionKey, subsection]) => {
            const subsectionTitle = subsection.title || formatFieldName(subsectionKey);

            html += `<div class="subsection"><h4>${subsectionTitle}</h4>`;

            // Process fields
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

            // Process items list
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

    // Special handling for medications section
    if (sectionKey === 'medications' || section.medications) {
        const medications = section.medications || section;
        if (Array.isArray(medications)) {
            html += '<div class="medications-container">';
            html += '<div class="medications-list">';

            medications.forEach(medication => {
                if (medication.name !== "Unavailable") {
                    html += `
                        <div class="medication-item">
                            <div class="medication-header">
                                <strong>${medication.name}</strong> - ${medication.dosage || ''}
                            </div>
                            <div class="medication-details">
                                ${medication.frequency || ''}
                                ${medication.duration ? ` · Duration: ${medication.duration}` : ''}
                                ${medication.medication_code ? ` · Code: ${medication.medication_code}` : ''}
                            </div>
                            ${medication.description ? `<div class="medication-description">${medication.description}</div>` : ''}
                        </div>
                    `;
                }
            });

            html += '</div>'; // Close medications-list
            html += '</div>'; // Close medications-container
        }
    }

    // Special handling for conditions
    if (sectionKey === 'other_conditions' || section.conditions) {
        const conditions = section.conditions || [];
        if (Array.isArray(conditions)) {
            html += '<div class="conditions-container">';
            html += '<div class="conditions-list">';

            conditions.forEach(condition => {
                html += `
                    <div class="condition-item">
                        <div class="condition-title">${condition.title || 'Condition'}</div>
                        ${condition.description ? `<div class="condition-description">${condition.description}</div>` : ''}
                    </div>
                `;
            });

            html += '</div>'; // Close conditions-list
            html += '</div>'; // Close conditions-container
        }
    }

    // Special handling for procedures
    if (sectionKey === 'requested_procedure' || section.procedures) {
        const procedures = section.procedures || [];
        if (Array.isArray(procedures)) {
            html += '<div class="procedures-container">';
            html += '<div class="procedures-list">';

            procedures.forEach(procedure => {
                html += `
                    <div class="procedure-item">
                        <span class="procedure-name">${procedure.name || 'Procedure'}</span>
                        ${procedure.cpt_code ? `<span class="procedure-code">CPT: ${procedure.cpt_code}</span>` : ''}
                    </div>
                `;
            });

            html += '</div>'; // Close procedures-list
            html += '</div>'; // Close procedures-container
        }
    }

    // Generic handling for properties not covered by specifics above
    for (const key in section) {
        if (key !== 'fields' && key !== 'subsections' && key !== 'title' &&
            key !== 'style' && key !== 'medications' && key !== 'conditions' &&
            key !== 'procedures' && typeof section[key] === 'object' &&
            section[key] !== null) {

            if (Array.isArray(section[key])) {
                html += processArrayProperty(key, section[key]);
            } else {
                html += processObjectProperty(key, section[key]);
            }
        }
    }

    return html;
}

/**
 * Process an array section
 */
function processArraySection(array, sectionKey) {
    let html = '';

    // Detect array type based on first item structure or section key
    if (array.length > 0) {
        if (sectionKey === 'medications' ||
            (typeof array[0] === 'object' && array[0] !== null &&
                (array[0].name !== undefined && array[0].dosage !== undefined))) {
            // Process as medications
            html += '<div class="medications-container">';
            html += '<div class="medications-list">';

            array.forEach(medication => {
                if (medication.name !== "Unavailable") {
                    html += `
                        <div class="medication-item">
                            <div class="medication-header">
                                <strong>${medication.name}</strong> - ${medication.dosage || ''}
                            </div>
                            <div class="medication-details">
                                ${medication.frequency || ''}
                                ${medication.duration ? ` · Duration: ${medication.duration}` : ''}
                                ${medication.medication_code ? ` · Code: ${medication.medication_code}` : ''}
                            </div>
                            ${medication.description ? `<div class="medication-description">${medication.description}</div>` : ''}
                        </div>
                    `;
                }
            });

            html += '</div>'; // Close medications-list
            html += '</div>'; // Close medications-container
        }
        else if (sectionKey === 'other_conditions' ||
            (typeof array[0] === 'object' && array[0] !== null &&
                (array[0].title !== undefined && array[0].description !== undefined))) {
            // Process as conditions
            html += '<div class="conditions-container">';
            html += '<div class="conditions-list">';

            array.forEach(condition => {
                html += `
                    <div class="condition-item">
                        <div class="condition-title">${condition.title || 'Condition'}</div>
                        ${condition.description ? `<div class="condition-description">${condition.description}</div>` : ''}
                    </div>
                `;
            });

            html += '</div>'; // Close conditions-list
            html += '</div>'; // Close conditions-container
        }
        else if (sectionKey === 'requested_procedure' ||
            (typeof array[0] === 'object' && array[0] !== null &&
                (array[0].name !== undefined && array[0].cpt_code !== undefined))) {
            // Process as procedures
            html += '<div class="procedures-container">';
            html += '<div class="procedures-list">';

            array.forEach(procedure => {
                html += `
                    <div class="procedure-item">
                        <span class="procedure-name">${procedure.name || 'Procedure'}</span>
                        ${procedure.cpt_code ? `<span class="procedure-code">CPT: ${procedure.cpt_code}</span>` : ''}
                    </div>
                `;
            });

            html += '</div>'; // Close procedures-list
            html += '</div>'; // Close procedures-container
        }
        else {
            // Generic array handling
            html += '<div class="generic-array-container">';
            html += `<h4 class="array-title">${formatSectionName(sectionKey)}:</h4>`;
            html += '<ul class="generic-array-list">';

            array.forEach(item => {
                if (typeof item === 'object' && item !== null) {
                    // Try to extract a name or title
                    const label = item.name || item.title || item.description || JSON.stringify(item);
                    html += `<li>${label}</li>`;
                } else {
                    html += `<li>${item}</li>`;
                }
            });

            html += '</ul>';
            html += '</div>';
        }
    }

    return html;
}

/**
 * Process a generic array property
 */
function processArrayProperty(key, array) {
    if (!array || !Array.isArray(array) || array.length === 0) {
        return '';
    }

    let html = `<div class="array-property">`;
    html += `<h4>${formatFieldName(key)}:</h4>`;
    html += `<ul class="array-property-list">`;

    array.forEach(item => {
        if (typeof item === 'object' && item !== null) {
            // Try to extract a meaningful property
            const displayValue = item.name || item.title || item.description ||
                item.value || JSON.stringify(item);
            html += `<li>${displayValue}</li>`;
        } else {
            html += `<li>${item}</li>`;
        }
    });

    html += `</ul>`;
    html += `</div>`;

    return html;
}

/**
 * Process a generic object property
 */
function processObjectProperty(key, obj) {
    if (!obj || typeof obj !== 'object') {
        return '';
    }

    let html = `<div class="object-property">`;
    html += `<h4>${formatFieldName(key)}:</h4>`;
    html += `<div class="object-property-content">`;

    // Process simple properties
    for (const propKey in obj) {
        if (typeof obj[propKey] !== 'object') {
            html += `
                <div class="field-item">
                    <span class="field-name">${formatFieldName(propKey)}:</span>
                    <span class="field-value">${obj[propKey]}</span>
                </div>
            `;
        }
    }

    html += `</div>`;
    html += `</div>`;

    return html;
}

/**
 * Add event listeners for section checkboxes and toggles
 */
function addSectionEventListeners(container) {
    // Add event listeners for section checkboxes
    container.querySelectorAll('.section-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const sectionId = this.getAttribute('data-section');
            const sectionPanel = document.getElementById(`section-${sectionId}`);

            if (this.checked) {
                if (!selectedSections.includes(sectionId)) {
                    selectedSections.push(sectionId);
                }
                if (sectionPanel) sectionPanel.classList.add('selected');
            } else {
                const index = selectedSections.indexOf(sectionId);
                if (index !== -1) {
                    selectedSections.splice(index, 1);
                }
                if (sectionPanel) sectionPanel.classList.remove('selected');
            }

            console.log('Selected sections:', selectedSections);
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

/**
 * Format section name for display
 */
function formatSectionName(sectionKey) {
    // Replace underscores with spaces
    let result = sectionKey.replace(/_/g, ' ');

    // Add spaces before capital letters
    result = result.replace(/([a-z])([A-Z])/g, '$1 $2');

    // Capitalize words
    return result.replace(/\b\w/g, l => l.toUpperCase());
}

/**
 * Format field name for display
 */
function formatFieldName(fieldName) {
    // Replace underscores with spaces
    let result = fieldName.replace(/_/g, ' ');

    // Add spaces before capital letters
    result = result.replace(/([a-z])([A-Z])/g, '$1 $2');

    // Capitalize words
    return result.replace(/\b\w/g, l => l.toUpperCase());
}

// Select all sections
function selectAllSections() {
    const checkboxes = document.querySelectorAll('.section-checkbox');
    selectedSections = [];

    checkboxes.forEach(checkbox => {
        checkbox.checked = true;
        const sectionId = checkbox.getAttribute('data-section');

        if (!selectedSections.includes(sectionId)) {
            selectedSections.push(sectionId);
        }

        const sectionPanel = document.getElementById(`section-${sectionId}`);
        if (sectionPanel) sectionPanel.classList.add('selected');
    });

    console.log('Selected all sections:', selectedSections);
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

    selectedSections = [];
    console.log('Deselected all sections');
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
// Approve selected sections
function approveSelectedSections() {
    console.log('Approving sections:', selectedSections);

    if (selectedSections.length === 0) {
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
    hdnSelectedNotes.value = JSON.stringify(selectedSections);
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

function enhanceClaimWithSelectionOptions(modalBody) {
    // Find the container with the claim data
    const claimContainer = modalBody.querySelector('.result-content');
    if (!claimContainer) return;

    try {
        // Parse the claim content if it's a string
        const claimContent = claimContainer.textContent;

        try {
            claimData = JSON.parse(claimContent);
        } catch (e) {
            console.error('Failed to parse claim data:', e);
            return;
        }


        // Clear the container
        claimContainer.innerHTML = '';

        // Format and add the claim data
        const formattedElement = formatInsuranceClaimData(claimData);
        claimContainer.appendChild(formattedElement);

        // Add event listeners to checkboxes for diagnosis selection
        const diagnosisCheckboxes = claimContainer.querySelectorAll('.diagnosis-checkbox');
        diagnosisCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                // You can add code here to track selected diagnoses
                console.log(`Diagnosis ${this.id} selected: ${this.checked}`);
            });
        });

    } catch (e) {
        console.error('Error enhancing claim with selection options:', e);
        // Restore original content if there's an error
        if (typeof claimData === 'string') {
            claimContainer.innerHTML = `<pre>${claimData}</pre>`;
        } else {
            claimContainer.innerHTML = `<pre>${JSON.stringify(claimData, null, 2)}</pre>`;
        }
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
    console.log("Currently selected notes:", modalState.selectedEnhancedNotes);

    if (modalState.selectedEnhancedNotes.length === 0) {
        // Try to select all checkboxes automatically if nothing is selected
        var allCheckboxes = document.querySelectorAll('.enhanced-note-checkbox');
        console.log("Found checkboxes:", allCheckboxes.length);

        if (allCheckboxes.length > 0) {
            // Auto-select all checkboxes
            allCheckboxes.forEach(function (checkbox) {
                checkbox.checked = true;
                var noteId = checkbox.getAttribute('data-index');
                var section = checkbox.getAttribute('data-section') || '';
                var noteKey = section ? section + '-' + noteId : noteId;

                if (!modalState.selectedEnhancedNotes.includes(noteKey)) {
                    modalState.selectedEnhancedNotes.push(noteKey);
                }
            });
            console.log("Auto-selected notes:", modalState.selectedEnhancedNotes);
        } else {
            // If no checkboxes found, use keys from the enhanced notes object
            try {
                var enhancedNotesField = document.getElementById('hdnEnhancedNotesData');
                if (enhancedNotesField && enhancedNotesField.value) {
                    var enhancedData = JSON.parse(enhancedNotesField.value);
                    if (enhancedData.sections) {
                        modalState.selectedEnhancedNotes = Object.keys(enhancedData.sections);
                        console.log("Auto-selected from data:", modalState.selectedEnhancedNotes);
                    }
                }
            } catch (e) {
                console.error("Error auto-selecting:", e);
            }
        }
    }

    // Check again after auto-selection
    if (modalState.selectedEnhancedNotes.length === 0) {
        alert('Please select at least one section to approve.');
        return;
    }

    // Get the hidden field
    var hdnField = document.getElementById('hdnSelectedNotes');
    console.log("Found hidden field:", hdnField !== null);

    if (hdnField) {
        // Store selections in hidden field
        hdnField.value = JSON.stringify(modalState.selectedEnhancedNotes);
        console.log("Stored in hidden field:", hdnField.value);
    } else {
        console.error("Hidden field not found!");
    }

    // Get the approve button
    var approveButton = document.getElementById('btnServerApproveNotes');
    console.log("Found approve button:", approveButton !== null);

    if (approveButton) {
        // Show loading indicator 
        var loadingIndicator = document.getElementById('loadingIndicator');
        if (loadingIndicator) loadingIndicator.style.display = 'block';

        // Close modal first to prevent visual glitches
        closeCurrentModal();

        // Small delay to allow UI to update
        setTimeout(function () {
            // Click the button to trigger server-side event
            approveButton.click();
        }, 100);
    } else {
        alert('System error: Could not find approve button. Please try again.');
    }
}

// Functions for handling note selection
function toggleNoteSelection(noteId) {
    var checkbox = document.getElementById('chk-' + noteId);
    var noteElement = document.getElementById('note-' + noteId);

    if (checkbox.checked) {
        noteElement.classList.add('selected');
        if (selectedNotes.indexOf(noteId) === -1) {
            selectedNotes.push(noteId);
        }
    } else {
        noteElement.classList.remove('selected');
        var index = selectedNotes.indexOf(noteId);
        if (index !== -1) {
            selectedNotes.splice(index, 1);
        }
    }

    // Enable/disable the approve button based on selection
    var approveButton = document.getElementById('btnApproveNotes');
    if (selectedNotes.length > 0) {
        approveButton.disabled = false;
    } else {
        approveButton.disabled = true;
    }
}

function toggleNoteSelectionByCheckbox(checkbox) {
    const noteId = checkbox.getAttribute('data-index');
    const section = checkbox.getAttribute('data-section');

    const noteKey = section ? `${section}-${noteId}` : noteId;

    // Log for debugging
    console.log('Toggle note:', noteKey, checkbox.checked);

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

    // Debug log
    console.log('Selected notes:', modalState.selectedEnhancedNotes);
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
//window.forceHideLoadingIndicator = function () {
//    // Direct DOM manipulation to hide the loading indicator
//    var loadingIndicator = document.getElementById('loadingIndicator');
//    if (loadingIndicator) {
//        loadingIndicator.style.display = 'none';
//    }

//    // Use a failsafe timer to ensure it stays hidden
//    //setTimeout(function () {
//    //    var loadingIndicator = document.getElementById('loadingIndicator');
//    //    if (loadingIndicator) {
//    //        loadingIndicator.style.display = 'none';
//    //    }
//    //}, 500);
//};

window.forceHideLoadingIndicator = function () {
    console.log("Force hiding loading indicator");

    // Try multiple methods to ensure it gets hidden
    var loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'none';
        loadingIndicator.style.visibility = 'hidden';
        loadingIndicator.style.opacity = '0';
    }

    // Use a redundant approach with jQuery if available
    if (typeof $ !== 'undefined') {
        $('#loadingIndicator').hide();
    }

    // Use a failsafe timer that continues trying to hide it
    var attempts = 0;
    var hideInterval = setInterval(function () {
        var indicator = document.getElementById('loadingIndicator');
        if (indicator) {
            indicator.style.display = 'none';
        }

        attempts++;
        if (attempts >= 5) {
            clearInterval(hideInterval);
        }
    }, 500);
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

window.showGeneratedClaimModal = function () {
    // Reset selections when opening
    modalState.selectedDiagnoses = [];
    showModal('pnlGeneratedClaim');
};

// Store and retrieve Request ID between operations
window.setCurrentRequestId = function (requestId) {
    currentRequestId = requestId;
    console.log("Current Request ID set to: " + requestId);

    // Store in localStorage for persistence across postbacks
    localStorage.setItem('currentRequestId', requestId);
};

// Get current request ID from various sources
window.getCurrentRequestId = function () {
    // Try variable first
    var requestId = currentRequestId;

    // If not in variable, try localStorage
    if (!requestId || requestId === 'undefined' || requestId === '') {
        requestId = localStorage.getItem('currentRequestId');
    }

    // If still not found, try to get it from UI elements
    if (!requestId || requestId === 'undefined' || requestId === '') {
        // Try request-info element
        var requestIdElement = document.querySelector('.request-info');
        if (requestIdElement) {
            var text = requestIdElement.textContent || requestIdElement.innerText;
            var match = text.match(/Request ID: ([a-zA-Z0-9-_]+)/);
            if (match && match[1]) {
                requestId = match[1];
                window.setCurrentRequestId(requestId);
            }
        }

        // Try hidden field if above failed
        if (!requestId || requestId === 'undefined' || requestId === '') {
            var hiddenField = document.getElementById('hdnRequestId');
            if (hiddenField && hiddenField.value) {
                requestId = hiddenField.value;
                window.setCurrentRequestId(requestId);
            }
        }
    }

    return requestId;
};

window.logDebug = function (message) {
    console.log("[ClaimKit Debug] " + message);

    // Optional: Log to a visible debug panel if you add one to your UI
    var debugPanel = document.getElementById('debugPanel');
    if (debugPanel) {
        var entry = document.createElement('div');
        entry.textContent = new Date().toISOString() + ": " + message;
        debugPanel.appendChild(entry);
    }
};

document.addEventListener('DOMContentLoaded', function () {
    // Create a more robust loading indicator management system
    window.loadingIndicator = {
        show: function () {
            var indicator = document.getElementById('loadingIndicator');
            if (indicator) {
                indicator.style.display = 'flex';
                console.log('Loading indicator shown');
            }
        },
        hide: function () {
            var indicator = document.getElementById('loadingIndicator');
            if (indicator) {
                indicator.style.display = 'none';
                console.log('Loading indicator hidden');
            }
        },
        isVisible: function () {
            var indicator = document.getElementById('loadingIndicator');
            return indicator && indicator.style.display !== 'none';
        }
    };

    // Override existing functions to ensure loading indicator is properly hidden
    var originalCloseModal = closeCurrentModal;
    window.closeCurrentModal = function () {
        // Call original function
        if (typeof originalCloseModal === 'function') {
            originalCloseModal();
        }

        // Always hide loading indicator when closing a modal
        window.loadingIndicator.hide();
    };

    // Add event handlers to intercept modal events
    document.body.addEventListener('click', function (e) {
        // If clicking a modal close button, hide loading indicator
        if (e.target.classList.contains('close-modal') ||
            e.target.parentElement.classList.contains('close-modal')) {
            window.setTimeout(window.loadingIndicator.hide, 100);
        }
    });

    // Add event listener for Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && window.loadingIndicator.isVisible()) {
            window.loadingIndicator.hide();
        }
    });

    // Add a safety timeout to hide the loading indicator if it stays visible too long
    window.setForceHideLoadingTimeout = function () {
        window.setTimeout(function () {
            if (window.loadingIndicator.isVisible()) {
                console.log('Force hiding loading indicator after timeout');
                window.loadingIndicator.hide();
            }
        }, 10000); // 10 seconds safety timeout
    };

    // Override the server endpoint callback functions
    var originalShowReviewResultsModal = window.showReviewResultsModal;
    window.showReviewResultsModal = function () {
        window.loadingIndicator.hide();
        if (typeof originalShowReviewResultsModal === 'function') {
            originalShowReviewResultsModal();
        }
    };

    var originalShowGeneratedClaimModal = window.showGeneratedClaimModal;
    window.showGeneratedClaimModal = function () {
        window.loadingIndicator.hide();
        if (typeof originalShowGeneratedClaimModal === 'function') {
            originalShowGeneratedClaimModal();
        }
    };

    var originalShowEnhancedNotesModal = window.showEnhancedNotesModal;
    window.showEnhancedNotesModal = function () {
        window.loadingIndicator.hide();
        if (typeof originalShowEnhancedNotesModal === 'function') {
            originalShowEnhancedNotesModal();
        }
    };

    // Force hide the loading indicator on page load, just in case
    window.loadingIndicator.hide();
});
