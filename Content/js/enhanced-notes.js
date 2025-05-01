// Enhanced Notes Functionality
$(document).ready(function () {
    // Initialize modal functionality if not already initialized
    if (typeof window.showEnhancedNotesModal !== 'function') {
        window.showEnhancedNotesModal = function () {
            // Create modal if it doesn't exist
            if (!$('#enhancedNotesModal').length) {
                createEnhancedNotesModal();
            }

            // Show the modal
            $('#enhancedNotesModal').addClass('show');
            $('body').addClass('modal-open');

            // Add click handler to close button if not already added
            $(document).off('click', '.enhanced-notes-modal .close-button').on('click', '.enhanced-notes-modal .close-button', function () {
                closeEnhancedNotesModal();
            });

            // Add click handler for overlay if not already added
            $(document).off('click', '.modal-overlay').on('click', '.modal-overlay', function (e) {
                if ($(e.target).hasClass('modal-overlay')) {
                    closeEnhancedNotesModal();
                }
            });
        };

        window.closeEnhancedNotesModal = function () {
            $('#enhancedNotesModal').removeClass('show');
            $('body').removeClass('modal-open');
        };

        function createEnhancedNotesModal() {
            // Create modal HTML
            var modalHtml = `
                <div id="enhancedNotesModal" class="modal enhanced-notes-modal">
                    <div class="modal-overlay"></div>
                    <div class="modal-container">
                        <div class="modal-header">
                            <h2>Enhanced Clinical Notes</h2>
                            <span class="close-button">×</span>
                        </div>
                        <div class="modal-body">
                            <!-- Content will be loaded here -->
                        </div>
                    </div>
                </div>
            `;
            // Append modal to body
            $('body').append(modalHtml);

            // Get content from ASP.NET panel and move it to modal
            var enhancedNotesContent = $('#<%=pnlEnhancedNotes.ClientID%> .result-content').html();
            $('#enhancedNotesModal .modal-body').html(enhancedNotesContent);

            // Add modal CSS if not already added
            if (!$('#enhancedNotesModalCSS').length) {
                var modalCSS = `
                    <style id="enhancedNotesModalCSS">
                        .enhanced-notes-modal .modal-container {
                            width: 90%;
                            max-width: 1200px;
                            max-height: 90vh;
                        }
                        
                        .enhanced-notes-modal .modal-body {
                            max-height: calc(90vh - 60px);
                            overflow-y: auto;
                        }
                    </style>
                `;
                $('head').append(modalCSS);
            }
        }
    }
});

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

function selectAllNotes() {
    var checkboxes = document.querySelectorAll('.note-checkbox');
    checkboxes.forEach(function (checkbox) {
        checkbox.checked = true;
        var noteId = checkbox.id.replace('chk-', '');
        var noteElement = document.getElementById('note-' + noteId);
        noteElement.classList.add('selected');
        if (selectedNotes.indexOf(noteId) === -1) {
            selectedNotes.push(noteId);
        }
    });

    // Enable the approve button
    var approveButton = document.getElementById('btnApproveNotes');
    if (approveButton) {
        approveButton.disabled = false;
    }
}

function deselectAllNotes() {
    var checkboxes = document.querySelectorAll('.note-checkbox');
    checkboxes.forEach(function (checkbox) {
        checkbox.checked = false;
        var noteId = checkbox.id.replace('chk-', '');
        var noteElement = document.getElementById('note-' + noteId);
        noteElement.classList.remove('selected');
    });

    // Clear selected notes array
    selectedNotes = [];

    // Disable the approve button
    var approveButton = document.getElementById('btnApproveNotes');
    if (approveButton) {
        approveButton.disabled = true;
    }
}

//function approveSelectedNotes() {
//    if (selectedNotes.length === 0) {
//        alert('Please select at least one section to approve.');
//        return;
//    }

//    // Store selections in hidden field
//    document.getElementById('<%=hdnSelectedNotes.ClientID%>').value = JSON.stringify(selectedNotes);

//    // Show loading indicator before submission
//    document.getElementById('loadingIndicator').style.display = 'block';

//    // Close the enhanced notes modal
//    closeEnhancedNotesModal();

//    // Trigger server-side approval
//    document.getElementById('<%=btnServerApproveNotes.ClientID%>').click();
//}

function approveSelectedNotes() {
    // Log for debugging
    console.log('Approving notes. Selected:', modalState.selectedEnhancedNotes);

    if (modalState.selectedEnhancedNotes.length === 0) {
        alert('Please select at least one section to approve.');
        return;
    }

    // Get the hidden field by EXACT server-generated ID
    var hdnField = document.getElementById('<%= hdnSelectedNotes.ClientID %>');
    // Or use constant if above doesn't work
    if (!hdnField) {
        hdnField = document.getElementById('hdnSelectedNotes');
    }

    console.log('Hidden field found:', hdnField !== null);

    // Store selections in hidden field
    if (hdnField) {
        hdnField.value = JSON.stringify(modalState.selectedEnhancedNotes);
        console.log('Stored in field:', hdnField.value);
    }

    // Show loading indicator before submission
    document.getElementById('loadingIndicator').style.display = 'block';

    // Get the button by EXACT server-generated ID
    var approveBtn = document.getElementById('<%= btnServerApproveNotes.ClientID %>');
    // Or use constant if above doesn't work
    if (!approveBtn) {
        approveBtn = document.getElementById('btnServerApproveNotes');
    }

    // Click the button to trigger server-side event
    if (approveBtn) {
        console.log('Clicking approve button');
        approveBtn.click();
    }

    closeCurrentModal();
}