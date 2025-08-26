<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ClaimKitv1.Default" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
    <head runat="server">
        <title>ClaimKit - Medical Documentation Assistant</title>
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <meta name="description" content="Advanced medical documentation assistant for healthcare professionals" />
        <meta name="keywords" content="medical documentation, healthcare, clinical notes, insurance claims" />
        
        <!-- Force Edge to use latest rendering mode -->
        <meta http-equiv="X-UA-Compatible" content="IE=edge">
        <!-- Prevent caching for development -->
        <meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate">
        <meta http-equiv="Pragma" content="no-cache">
        <meta http-equiv="Expires" content="0">
    
        <!-- Preload critical resources -->
        <%--<link rel="preload" href="Content/css/styles.css" as="style">
        <link rel="preload" href="Content/css/themes/classic-ui.css" as="style">--%>
        
        <!-- Stylesheets -->
        <link rel="stylesheet" href="Content/css/styles.css?v=2.0" />
        <link rel="stylesheet" href="Content/css/themes/classic-ui.css?v=2.0" />
        
        <!-- Core JavaScript Libraries -->
        <script src="https://ajax.aspnetcdn.com/ajax/jquery/jquery-3.6.0.min.js" type="text/javascript"></script>
        <script src="https://ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js" type="text/javascript"></script>
        
       
    </head>

    <body class="classic-ui">
        <form id="form1" runat="server" novalidate>
            <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" ScriptMode="Release" EnablePartialRendering="true">
                <Scripts>
                    <asp:ScriptReference Path="https://ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js" />
                </Scripts>
            </asp:ScriptManager>

            <!-- Header Section -->
            <header class="header" role="banner">
                <div class="container">
                    <h1>
                        ClaimKit Assistant
                        <span class="version-badge">v3.0</span>
                    </h1>
                </div>
            </header>

            <main class="container" role="main">

                <!-- Loading Indicator -->
                <div id="loadingIndicator" class="loading-indicator" role="status" aria-live="polite" aria-label="Processing request">
                    <div class="spinner" aria-hidden="true"></div>
                    <div class="loading-text">Processing your request... Please wait</div>
                    <div class="loading-progress">
                        <div class="progress-bar" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100"></div>
                    </div>
                </div>

                <!-- Form Section -->
                <section class="form-section" role="form" aria-labelledby="patient-info-heading">
                    <div class="btn-group">
                        <asp:Button ID="btnLoadEmrData" runat="server" Text="🔄Load Patient Data from EMR" 
                                    CssClass="btn btn-secondary action-button" OnClick="btnLoadEmrData_Click" 
                                    ToolTip="Load patient data from EMR system" />
                        <asp:Button ID="btnClearEmrData" runat="server" Text="🗑️ Clear EMR Data" 
                                    CssClass="btn btn-outline action-button" OnClick="btnClearEmrData_Click" 
                                    ToolTip="Clear loaded EMR data" Visible="false" />
                    </div>
                    <!-- Patient Identification -->
                    <div class="form-row">
                        <div class="form-group">
                            <label for="txtPatientId" class="required">Patient ID:</label>
                            <asp:TextBox ID="txtPatientId" runat="server" CssClass="form-control"
                                placeholder="Enter patient ID or encounter ID" 
                                aria-describedby="txtPatientId-help" 
                                data-validation="required"></asp:TextBox>
                            <label for="txtEncounterId" class="required">Encounter ID:</label>
                            <asp:TextBox ID="txtEncounterId" runat="server" CssClass="form-control"
                                placeholder="Enter encounter ID" 
                                data-validation="required,numeric"
                                aria-describedby="txtEncounterId-help"></asp:TextBox>
                            <label for="txtRegistrationId" class="required">Registration ID:</label>
                            <asp:TextBox ID="txtRegistrationId" runat="server" CssClass="form-control"
                                placeholder="Enter registration ID" 
                                data-validation="required,numeric"
                                aria-describedby="txtRegistrationId-help"></asp:TextBox>
                            <label for="txtHospitalLocationId">Hospital Location ID:</label>
                            <asp:TextBox ID="txtHospitalLocationId" runat="server" CssClass="form-control" 
                                placeholder="Enter hospital location ID (optional)" 
                                data-validation="numeric"
                                aria-describedby="txtHospitalLocationId-help"></asp:TextBox>
                        </div>
                        <div class="form-group">
                            <label for="txtInsuranceCompany" class="required">Insurance Company:</label>
                            <asp:TextBox ID="txtInsuranceCompany" runat="server" CssClass="form-control" 
                                placeholder="Enter insurance company name" 
                                data-validation="required"
                                aria-describedby="txtInsuranceCompany-help"></asp:TextBox>
                            <label for="ddlPolicyBand" class="required">Policy Type:</label>
                            <asp:DropDownList ID="ddlPolicyBand" runat="server" CssClass="form-control" 
                                data-validation="required" aria-describedby="ddlPolicyBand-help">
                                <asp:ListItem Value="" Text="-- Select Policy Type --" />
                                <asp:ListItem Value="Basic" Text="Basic" />
                                <asp:ListItem Value="Standard" Text="Standard" />
                                <asp:ListItem Value="Silver" Text="Silver" />
                                <asp:ListItem Value="Gold" Text="Gold" />
                                <asp:ListItem Value="Platinum" Text="Platinum" />
                            </asp:DropDownList>
                            <asp:TextBox ID="txtPolicyBand" runat="server" CssClass="form-control form-control-fallback" 
                                placeholder="Or enter custom policy type" 
                                Style="display: none;" />
                            <label for="txtPolicyId" class="required">Policy ID:</label>
                            <asp:TextBox ID="txtPolicyId" runat="server" CssClass="form-control" 
                                placeholder="Enter policy ID or member number" 
                                data-validation="required"
                                aria-describedby="txtPolicyId-help"></asp:TextBox>
                        </div>
                        <div class="form-group">
                            <label for="txtTemplateId">Template ID:</label>
                            <asp:TextBox ID="txtTemplateId" runat="server" CssClass="form-control" 
                                placeholder="Enter template ID (optional)" 
                                data-validation="numeric"
                                aria-describedby="txtTemplateId-help"></asp:TextBox>
                            <label for="txtDoctorName" class="required">Clinician Name:</label>
                            <asp:TextBox ID="txtDoctorName" runat="server" CssClass="form-control" 
                                placeholder="Enter your full name" 
                                data-validation="required"
                                aria-describedby="txtDoctorName-help"></asp:TextBox>
                            <label for="txtDoctorSpecialization" class="required">Specialization:</label>
                            <asp:TextBox ID="txtDoctorSpecialization" runat="server" CssClass="form-control" 
                                placeholder="Enter your medical specialization" 
                                data-validation="required"
                                aria-describedby="txtDoctorSpecialization-help"></asp:TextBox>
                            <label for="txtDoctorId" class="required">Clinician ID:</label>
                            <asp:TextBox ID="txtDoctorId" runat="server" CssClass="form-control" 
                                placeholder="Enter your clinician ID" 
                                data-validation="required"
                                aria-describedby="txtDoctorId-help"></asp:TextBox>
                        </div>
                        <div class="form-group">
                            <label for="txtPatientHistory">Patient History:</label>
                            <asp:TextBox ID="txtPatientHistory" runat="server" CssClass="form-control" 
                                TextMode="MultiLine" Rows="6" 
                                placeholder='Enter patient history in JSON format or free text. Example: [{"date": "MM/DD/YYYY", "doctor": "Dr. Name", "diagnosis": "Diagnosis", "treatment": "Treatment"}]'
                                aria-describedby="txtPatientHistory-help"
                                data-auto-save="true"></asp:TextBox>
                        </div>
                        <div class="form-group">
                            <label for="txtDoctorNotes" class="required">Clinical Notes: <span class="required-indicator">*</span></label>
                            <asp:TextBox ID="txtDoctorNotes" runat="server" CssClass="form-control clinical-notes-editor" 
                                TextMode="MultiLine" Rows="12" 
                                placeholder="Enter your detailed clinical notes here. Include patient presentation, examination findings, assessment, and treatment plan."
                                data-validation="required"
                                data-auto-save="true"
                                aria-describedby="txtDoctorNotes-help"></asp:TextBox>
                        </div>
                    </div>
                    <div class="btn-group primary-actions">
                        <asp:Button ID="btnReviewNotes" runat="server" Text="🔍 Enhance Clinical Notes" 
                                    CssClass="btn btn-primary action-button" OnClick="btnReviewNotes_Click" 
                                    Visible="false" />
                        <asp:Button ID="btnViewEnhanceNotes" runat="server"
                                    Text="View & Enhance Notes"
                                    CssClass="btn btn-primary action-button"
                                    OnClick="btnViewEnhanceNotes_Click" />

                    </div>
                </section>

                <!-- Update Panel for Dynamic Content -->
                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <!-- Hidden Fields -->
                        <asp:HiddenField ID="hdnShowModal" runat="server" Value="" />
                        <asp:HiddenField ID="hdnEnhancedNotesData" runat="server" />
                        <asp:HiddenField ID="hdnSelectedNotes" runat="server" />
                        <asp:HiddenField ID="hdnRequestId" runat="server" />
                        <asp:HiddenField ID="hdnSelectedDiagnoses" runat="server" />

                        <!-- Action Buttons Section -->
                        <asp:Panel ID="pnlActionButtons" runat="server" CssClass="action-buttons" Visible="false">
                            <div class="btn-group secondary-actions">
                                <%--<asp:Button ID="btnViewResults" runat="server" Text="View Review Results" 
                                    CssClass="btn btn-info action-button" 
                                    OnClientClick="showReviewResultsModal(); return false;" />--%>
                                <asp:Button ID="btnViewDetailedReview" runat="server" Text="View Detailed Review" 
                                    CssClass="btn btn-outline action-button" 
                                    OnClientClick="showReviewResultsModal(); return false;" 
                                    Visible="false" ToolTip="View detailed review results (admin/debug mode)" />
                            </div>
                        </asp:Panel>

                        <!-- Review Results Panel -->
                        <asp:Panel ID="pnlReviewResults" runat="server" CssClass="result-panel" Visible="false">
                            <div class="panel-header">
                                <h3>Clinical Notes Review</h3>
                            </div>
                            <div class="review-info">
                                <asp:Label ID="lblStatus" runat="server" CssClass="status-info"></asp:Label>
                                <asp:Label ID="lblRequestId" runat="server" CssClass="request-info"></asp:Label>
                            </div>
                            <div class="categories-container">
                                <asp:Repeater ID="rptReviewCategories" runat="server" OnItemDataBound="rptReviewCategories_ItemDataBound">
                                    <ItemTemplate>
                                        <div class="category-item">
                                            <asp:Literal ID="litCategoryContent" runat="server"></asp:Literal>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </asp:Panel>

                        <!-- Enhanced Notes Panel -->
                        <asp:Panel ID="pnlEnhancedNotes" runat="server" CssClass="result-panel" Visible="false">
                            <div class="result-content">
                                <asp:Literal ID="litEnhancedNotes" runat="server"></asp:Literal>
                            </div>
                            <asp:Button ID="btnServerApproveNotes" runat="server" Text="Approve Notes" 
                                CssClass="hidden-button" OnClick="btnServerApproveNotes_Click" Style="display: none;" />
                        </asp:Panel>

                        <!-- Final Notes Panel -->
                        <asp:Panel ID="pnlFinalNotes" runat="server" CssClass="result-panel final-notes-panel" Visible="false">
                            <div class="form-group">
                                <label for="txtFinalNotes">Final Clinical Notes:</label>
                                <asp:TextBox ID="txtFinalNotes" runat="server" CssClass="form-control final-notes-editor" 
                                    TextMode="MultiLine" Rows="20" data-auto-save="true"
                                    aria-describedby="txtFinalNotes-help"></asp:TextBox>
                            </div>

                            <div class="btn-group final-actions">
                                <asp:Button ID="btnSaveFinalNotes" runat="server" Text="💾 Save & Submit Documentation" 
                                    CssClass="btn btn-primary action-button" OnClick="btnSaveFinalNotes_Click" />
                                <button type="button" id="btnBackToEnhanced" class="btn btn-outline action-button" 
                                    onclick="goBackToEnhanced();" aria-label="Go back to enhanced notes">
                                    <span aria-hidden="true">←</span> Back to Enhanced Notes
                                </button>
                            </div>
                        </asp:Panel>

                        <!-- Error Display Panel -->
                        <asp:Panel ID="pnlError" runat="server" CssClass="error-panel" Visible="false" role="alert">
                            <div class="error-header">
                                <span class="error-title">System Message</span>
                                <asp:LinkButton ID="btnCloseError" runat="server" CssClass="close-button" 
                                    OnClick="btnCloseError_Click" Text="×" aria-label="Close error message"></asp:LinkButton>
                            </div>
                            <div class="error-content">
                                <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message"></asp:Label>

                                <asp:Panel ID="pnlJsonContent" runat="server" CssClass="json-content" Visible="false">
                                    <div class="json-header">
                                        <span>Technical Details</span>
                                        <asp:LinkButton ID="btnExpandCollapse" runat="server" CssClass="expand-collapse" 
                                            OnClientClick="toggleJsonContent(); return false;">Show Details</asp:LinkButton>
                                    </div>
                                    <pre id="preFormattedJson" runat="server" class="formatted-json"></pre>
                                </asp:Panel>
                            </div>
                        </asp:Panel>

                        <!-- Confirmation Panel -->
                        <asp:Panel ID="pnlConfirmation" runat="server" CssClass="confirmation-panel" Visible="false" role="alert">
                            <div class="confirmation-header">
                                <span class="confirmation-title">✅ Success</span>
                                <asp:LinkButton ID="btnCloseConfirmation" runat="server" CssClass="close-button" 
                                    OnClick="btnCloseConfirmation_Click" Text="×" aria-label="Close confirmation message"></asp:LinkButton>
                            </div>
                            <div class="confirmation-content">
                                <div class="confirmation-icon" aria-hidden="true">✓</div>
                                <asp:Label ID="lblConfirmationMessage" runat="server" CssClass="confirmation-message"></asp:Label>

                                <div class="confirmation-actions">
                                    <asp:Button ID="btnConfirmationOk" runat="server" Text="OK" 
                                        CssClass="btn btn-primary" OnClick="btnCloseConfirmation_Click" />
                                </div>
                            </div>
                        </asp:Panel>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="btnLoadEmrData" EventName="Click" />
                        <asp:AsyncPostBackTrigger ControlID="btnClearEmrData" EventName="Click" />
                        <asp:AsyncPostBackTrigger ControlID="btnReviewNotes" EventName="Click" />
                        <asp:AsyncPostBackTrigger ControlID="btnServerApproveNotes" EventName="Click" />
                        <asp:AsyncPostBackTrigger ControlID="btnSaveFinalNotes" EventName="Click" />
                    </Triggers>
                </asp:UpdatePanel>

                <!-- Client-side validation container -->
                <div id="validationErrorContainer" class="validation-errors" style="display: none;" role="alert" aria-live="assertive"></div>
            </main>

            <!-- Footer -->
            <footer class="footer" role="contentinfo">
                <div class="container">
                    <div class="footer-content">
                        <div class="footer-info">
                            <span>&copy; 2025 ClaimKit Assistant</span>
                            <span class="version">Version 3.0</span>
                        </div>
                        <%--<div class="footer-links">
                            <a href="#" onclick="ClaimKit.UI.showPrivacyPolicy()">Privacy Policy</a>
                            <a href="#" onclick="ClaimKit.UI.showTerms()">Terms of Use</a>
                            <a href="#" onclick="ClaimKit.UI.showSupport()">Support</a>
                        </div>--%>
                    </div>
                </div>
            </footer>

            <!-- Core JavaScript Files -->
            <script src="Content/js/claimkit.js?v=2.0" defer></script>
            <script src="Content/js/claimkit-modal.js?v=2.0" defer></script>

            

            <!-- Enhanced JavaScript Functions -->
            <script type="text/javascript">
                // Enhanced final notes functions
                function insertTimestamp() {
                    try {
                        const txtArea = document.getElementById('<%=txtFinalNotes.ClientID%>');
                        if (!txtArea) return;

                        const date = new Date();
                        const timestamp = date.toLocaleString();
                        const cursorPos = txtArea.selectionStart;
                        const textBefore = txtArea.value.substring(0, cursorPos);
                        const textAfter = txtArea.value.substring(cursorPos);

                        txtArea.value = textBefore + "[" + timestamp + "] " + textAfter;
                        txtArea.focus();
                        const newPos = cursorPos + timestamp.length + 3;
                        txtArea.setSelectionRange(newPos, newPos);

                        // Trigger auto-save
                        if (typeof ClaimKit !== 'undefined' && ClaimKit.AutoSave) {
                            ClaimKit.AutoSave.saveField(txtArea);
                        }
                    } catch (error) {
                        console.error('Error inserting timestamp:', error);
                    }
                }

                function insertSignature() {
                    try {
                        const txtArea = document.getElementById('<%=txtFinalNotes.ClientID%>');
                        if (!txtArea) return;

                        const doctorName = document.getElementById('<%=txtDoctorName.ClientID%>').value || '[Doctor Name]';
                        const specialization = document.getElementById('<%=txtDoctorSpecialization.ClientID%>').value || '[Specialization]';
                        const signature = `\n\n---\nDocumented by: ${doctorName}, ${specialization}\nDate: ${new Date().toLocaleDateString()}\nTime: ${new Date().toLocaleTimeString()}\n`;

                        txtArea.value += signature;
                        txtArea.focus();
                        txtArea.scrollTop = txtArea.scrollHeight;
            
                        // Trigger auto-save
                        if (typeof ClaimKit !== 'undefined' && ClaimKit.AutoSave) {
                            ClaimKit.AutoSave.saveField(txtArea);
                        }
                    } catch (error) {
                        console.error('Error inserting signature:', error);
                    }
                }

                function goBackToEnhanced() {
                    try {
                        // Save current text for persistence
                        const finalNotesField = document.getElementById('<%=txtFinalNotes.ClientID%>');
                        if (finalNotesField && finalNotesField.value) {
                            sessionStorage.setItem('claimkit_finalNotesBackup', finalNotesField.value);
                        }

                        // Show enhanced notes modal directly
                        if (typeof window.showEnhancedNotesModal === 'function') {
                            window.showEnhancedNotesModal();
                        } else {
                            console.error('showEnhancedNotesModal function not found');
                        }

                        return false; // Prevent form submission
                    } catch (error) {
                        console.error('Error going back to enhanced notes:', error);
                        return false;
                    }
                }

                // Enhanced modal system for direct workflow
                document.addEventListener('DOMContentLoaded', function () {
                    try {
                        // Enhanced showEnhancedNotesModal function - immediate display for direct workflow
                        window.showEnhancedNotesModal = function () {
                            console.log("ClaimKit: Enhanced notes modal called - Direct workflow");

                            // Hide loading indicator immediately
                            const loadingIndicator = document.getElementById('loadingIndicator');
                            if (loadingIndicator) {
                                loadingIndicator.style.display = 'none';
                            }

                            // Get request ID from multiple sources
                            const requestId = document.getElementById('hdnRequestId')?.value ||
                                localStorage.getItem('currentRequestId') || '';

                            console.log("ClaimKit: Request ID:", requestId);

                            // Find or create enhanced notes panel
                            let enhancedNotesPanel = document.getElementById('pnlEnhancedNotes');
                            if (!enhancedNotesPanel) {
                                console.log("ClaimKit: Creating enhanced notes panel");
                                enhancedNotesPanel = document.createElement('div');
                                enhancedNotesPanel.id = 'pnlEnhancedNotes';
                                enhancedNotesPanel.className = 'result-panel';
                                enhancedNotesPanel.style.display = 'none';
                                enhancedNotesPanel.innerHTML = '<div class="result-content"></div>';
                                document.body.appendChild(enhancedNotesPanel);
                            }

                            // Get enhanced notes data with multiple fallback attempts
                            const enhancedNotesData = document.getElementById('hdnEnhancedNotesData');

                            if (enhancedNotesData && enhancedNotesData.value) {
                                console.log("ClaimKit: Enhanced notes data found, showing immediately");
                                displayEnhancedNotesContent(enhancedNotesPanel, enhancedNotesData.value);
                                showModalImmediate('pnlEnhancedNotes');
                            } else {
                                console.log("ClaimKit: Enhanced notes data not ready, attempting fallback");

                                // Try to find data in the panel itself
                                const existingContent = enhancedNotesPanel.querySelector('.result-content');
                                const dataContainer = existingContent?.querySelector('#enhancedNotesDataContainer');

                                if (dataContainer && dataContainer.getAttribute('data-json')) {
                                    console.log("ClaimKit: Found data in existing container");
                                    displayEnhancedNotesContent(enhancedNotesPanel, dataContainer.getAttribute('data-json'));
                                    showModalImmediate('pnlEnhancedNotes');
                                } else {
                                    // Show loading state and retry
                                    showLoadingModal(enhancedNotesPanel);
                                    retryEnhancedNotesModal();
                                }
                            }
                        };

                        // Helper function to display enhanced notes content
                        function displayEnhancedNotesContent(panel, jsonData) {
                            const resultContent = panel.querySelector('.result-content');
                            if (resultContent) {
                                resultContent.innerHTML = '';

                                // Create data container
                                const dataContainer = document.createElement('div');
                                dataContainer.id = 'enhancedNotesDataContainer';
                                dataContainer.style.display = 'none';
                                dataContainer.setAttribute('data-json', jsonData);

                                // Create display container
                                const displayContainer = document.createElement('div');
                                displayContainer.id = 'enhancedNotesDisplayContainer';

                                resultContent.appendChild(dataContainer);
                                resultContent.appendChild(displayContainer);

                                // Process the enhanced notes for display
                                enhanceNotesWithSelectionOptions(resultContent);
                            }
                        }

                        // Helper function to show modal immediately
                        function showModalImmediate(panelId) {
                            if (typeof window.showModal === 'function') {
                                window.showModal(panelId);
                            } else {
                                console.error("showModal function not found");
                                // Fallback: try to show the modal directly
                                const modal = document.getElementById(`modal-${panelId}`);
                                if (modal) {
                                    modal.classList.add('show');
                                    document.body.style.overflow = 'hidden';
                                }
                            }
                        }

                        // Helper function to show loading modal
                        function showLoadingModal(panel) {
                            const resultContent = panel.querySelector('.result-content');
                            if (resultContent) {
                                resultContent.innerHTML = `
                                    <div class="loading-message">
                                        <div class="spinner"></div>
                                        <p>Loading enhanced notes...</p>
                                    </div>
                                `;
                            }
                            showModalImmediate('pnlEnhancedNotes');
                        }

                        // Retry function for enhanced notes modal
                        function retryEnhancedNotesModal() {
                            let attempts = 0;
                            const maxAttempts = 10;
                            const retryInterval = 500;

                            const retryTimer = setInterval(function () {
                                attempts++;
                                console.log(`ClaimKit: Retry attempt ${attempts} for enhanced notes data`);

                                const enhancedNotesData = document.getElementById('hdnEnhancedNotesData');
                                if (enhancedNotesData && enhancedNotesData.value) {
                                    clearInterval(retryTimer);
                                    console.log("ClaimKit: Enhanced notes data found on retry");

                                    const panel = document.getElementById('pnlEnhancedNotes');
                                    if (panel) {
                                        displayEnhancedNotesContent(panel, enhancedNotesData.value);
                                    }
                                } else if (attempts >= maxAttempts) {
                                    clearInterval(retryTimer);
                                    console.error("ClaimKit: Failed to load enhanced notes after maximum attempts");

                                    const panel = document.getElementById('pnlEnhancedNotes');
                                    const resultContent = panel?.querySelector('.result-content');
                                    if (resultContent) {
                                        resultContent.innerHTML = `
                                            <div class="error-message" style="text-align: center; padding: 2rem; color: #d32f2f;">
                                                <p>Enhanced notes are taking longer than expected.</p>
                                                <p>Please try refreshing the page or contact support if the issue persists.</p>
                                                <button type="button" onclick="location.reload()" class="btn btn-primary" style="margin-top: 1rem;">
                                                    Refresh Page
                                                </button>
                                            </div>
                                        `;
                                    }
                                }
                            }, retryInterval);
                        }

                        // Keep the review results modal function for optional use
                        window.showReviewResultsModal = function () {
                            console.log("ClaimKit: Review results modal called");

                            // Hide loading indicator
                            const loadingIndicator = document.getElementById('loadingIndicator');
                            if (loadingIndicator) {
                                loadingIndicator.style.display = 'none';
                            }

                            // Show the review results modal
                            if (typeof window.showModal === 'function') {
                                window.showModal('pnlReviewResults');
                            }
                        };

                        console.log('ClaimKit: Enhanced modal system initialized for direct workflow');
                    } catch (error) {
                        console.error('ClaimKit: Modal initialization error:', error);
                    }
                });

                // Add CSS for loading spinner animation
                const style = document.createElement('style');
                style.textContent = `
                        @keyframes spin {
                            0% { transform: rotate(0deg); }
                            100% { transform: rotate(360deg); }
                        }
                    `;
                document.head.appendChild(style);

                // Add CSS for loading spinner
                document.addEventListener('DOMContentLoaded', function () {
                    const style = document.createElement('style');
                    style.textContent = `
                            @keyframes spin {
                                0% { transform: rotate(0deg); }
                                100% { transform: rotate(360deg); }
                            }
                        `;
                    document.head.appendChild(style);
                });
            </script>
        </form>
    </body>
</html>