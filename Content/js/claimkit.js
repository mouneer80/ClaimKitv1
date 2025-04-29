// JavaScript for toggling JSON content and formatting
function toggleJsonContent() {
    const content = document.querySelector('.json-content');
    const preElement = document.querySelector('.formatted-json');
    const button = document.querySelector('.expand-collapse');

    if (content.classList.contains('expanded')) {
        content.classList.remove('expanded');
        preElement.classList.add('collapsed');
        button.textContent = 'Expand';
    } else {
        content.classList.add('expanded');
        preElement.classList.remove('collapsed');
        button.textContent = 'Collapse';
    }
}

// Format JSON with syntax highlighting
function formatJson(jsonString) {
    try {
        // Parse the JSON string
        const json = JSON.parse(jsonString);

        // Format it with proper indentation
        const formattedJson = JSON.stringify(json, null, 2);

        // Apply syntax highlighting
        return syntaxHighlight(formattedJson);
    } catch (e) {
        // If it's not valid JSON, return the original string
        console.error("JSON parsing error:", e);
        return jsonString;
    }
}

// Apply syntax highlighting to JSON
function syntaxHighlight(json) {
    // Escape HTML special characters
    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    // Apply CSS classes to different parts of the JSON
    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g,
        function (match) {
            let cls = 'json-number';
            if (/^"/.test(match)) {
                if (/:$/.test(match)) {
                    cls = 'json-key';
                } else {
                    cls = 'json-string';
                }
            } else if (/true|false/.test(match)) {
                cls = 'json-boolean';
            } else if (/null/.test(match)) {
                cls = 'json-null';
            }
            return '<span class="' + cls + '">' + match + '</span>';
        }
    );
}

// Check if a string is valid JSON
function isJsonString(str) {
    try {
        JSON.parse(str);
        return true;
    } catch (e) {
        return false;
    }
}
function showTab(tabName) {
    // Hide all tab contents
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });

    // Deactivate all tab buttons
    document.querySelectorAll('.tab-button').forEach(button => {
        button.classList.remove('active');
    });

    // Show selected tab content
    document.getElementById('tab-' + tabName).classList.add('active');

    // Activate selected tab button
    document.querySelector('.tab-button[onclick*="' + tabName + '"]').classList.add('active');
}

function toggleCategory(categoryId) {
    const content = document.getElementById(categoryId);
    const header = content.previousElementSibling;
    const icon = header.querySelector('.toggle-icon');

    if (content.classList.contains('expanded')) {
        content.classList.remove('expanded');
        icon.classList.remove('expanded');
        icon.textContent = '+';
    } else {
        content.classList.add('expanded');
        icon.classList.add('expanded');
        icon.textContent = '×';
    }
}

// Initialize all categories closed on page load
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.toggle-icon').forEach(icon => {
        icon.textContent = '+';
    });
});

// Extract JSON from a string that might contain other text
function extractJson(text) {
    // Try to find objects (starting with { and ending with })
    const objectRegex = /(\{[\s\S]*\})/;
    const objectMatch = text.match(objectRegex);

    if (objectMatch && isJsonString(objectMatch[1])) {
        return objectMatch[1];
    }

    // Try to find arrays (starting with [ and ending with ])
    const arrayRegex = /(\[[\s\S]*\])/;
    const arrayMatch = text.match(arrayRegex);

    if (arrayMatch && isJsonString(arrayMatch[1])) {
        return arrayMatch[1];
    }

    return null;
}

$(document).ready(function () {
    // Add click handlers to category headers
    $(document).on('click', '.category-header', function () {
        $(this).toggleClass('collapsed');

        // Adjust content height for animation
        var content = $(this).next('.category-content');
        if ($(this).hasClass('collapsed')) {
            // Save original height before collapsing
            content.attr('data-height', content.height());
            content.css('max-height', '0');
        } else {
            // Restore to original height
            var originalHeight = content.attr('data-height') || '1000px';
            content.css('max-height', originalHeight + 'px');
        }
    });

    // Initialize all categories as expanded
    $('.category-header').each(function () {
        var content = $(this).next('.category-content');
        content.attr('data-height', content.height());
    });
});

document.addEventListener('DOMContentLoaded', function () {
    // Modal functionality
    const modals = document.querySelectorAll('.modal');
    const openModalButtons = document.querySelectorAll('[data-modal-target]');
    const closeModalButtons = document.querySelectorAll('.close-modal');

    openModalButtons.forEach(button => {
        button.addEventListener('click', () => {
            const modal = document.querySelector(button.dataset.modalTarget);
            openModal(modal);
        });
    });

    closeModalButtons.forEach(button => {
        button.addEventListener('click', () => {
            const modal = button.closest('.modal');
            closeModal(modal);
        });
    });

    modals.forEach(modal => {
        modal.addEventListener('click', e => {
            if (e.target === modal) {
                closeModal(modal);
            }
        });
    });

    function openModal(modal) {
        if (modal == null) return;
        modal.style.display = 'block';
    }

    function closeModal(modal) {
        if (modal == null) return;
        modal.style.display = 'none';
    }

    // Form validation
    const reviewForm = document.getElementById('reviewForm');

    if (reviewForm) {
        reviewForm.addEventListener('submit', function (event) {
            const doctorNotes = document.getElementById('txtDoctorNotes');
            const patientHistory = document.getElementById('txtPatientHistory');
            const errorContainer = document.getElementById('validationErrorContainer');

            // Clear previous errors
            errorContainer.innerHTML = '';
            errorContainer.style.display = 'none';

            let isValid = true;
            let errorMessages = [];

            // Check required fields
            if (!doctorNotes.value.trim()) {
                errorMessages.push('Doctor notes are required');
                isValid = false;
            }

            // Validate JSON format for patient history
            if (patientHistory.value.trim()) {
                try {
                    JSON.parse(patientHistory.value);
                } catch (e) {
                    errorMessages.push('Patient history must be valid JSON');
                    isValid = false;
                }
            }

            // Display errors if any
            if (!isValid) {
                event.preventDefault();
                errorContainer.innerHTML = errorMessages.map(msg => `<div>${msg}</div>`).join('');
                errorContainer.style.display = 'block';
            }
        });
    }

    // Initialize JSON editor for patient history
    const patientHistoryTextarea = document.getElementById('txtPatientHistory');

    if (patientHistoryTextarea) {
        // Format JSON on focus out
        patientHistoryTextarea.addEventListener('blur', function () {
            try {
                const json = JSON.parse(this.value);
                this.value = JSON.stringify(json, null, 4);
            } catch (e) {
                // If not valid JSON, leave as is
            }
        });
    }

    // Show loading indicator when performing async operations
    const actionButtons = document.querySelectorAll('.action-button');
    const loadingIndicator = document.getElementById('loadingIndicator');

    if (actionButtons && loadingIndicator) {
        actionButtons.forEach(button => {
            button.addEventListener('click', function () {
                loadingIndicator.style.display = 'block';
            });
        });
    }

    // Show tooltips for fields with additional information
    const tooltips = document.querySelectorAll('[data-tooltip]');

    tooltips.forEach(element => {
        const tooltip = document.createElement('div');
        tooltip.className = 'tooltip';
        tooltip.textContent = element.dataset.tooltip;

        element.appendChild(tooltip);

        element.addEventListener('mouseenter', () => {
            tooltip.style.display = 'block';
        });

        element.addEventListener('mouseleave', () => {
            tooltip.style.display = 'none';
        });
    });
});