<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ClaimKitv1.Default" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ClaimKit - Medical Documentation Assistant</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="Content/css/styles.css" />
    <link rel="stylesheet" href="Content/css/modal-styles.css" />
    <!-- Make sure these script references are included -->
    <script src="https://ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js" type="text/javascript"></script>
    <script src="https://ajax.aspnetcdn.com/ajax/jquery/jquery-3.6.0.min.js" type="text/javascript"></script>
</head>

<body>
    <form id="form1" runat="server">
        <!-- Add these references to your page head -->
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" ScriptMode="Release">
            <Scripts>
                <asp:ScriptReference Path="https://ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js" />
            </Scripts>
        </asp:ScriptManager>

        <div class="header">
            <div class="container">
                <h1>ClaimKit - Medical Documentation Assistant</h1>
            </div>
        </div>
        <div class="container">
            <!-- Loading Indicator -->
            <div id="loadingIndicator" style="display: none;" class="loading-indicator">
                <div class="spinner"></div>
                <div>Processing your request... Please wait</div>
            </div>

            <!-- Form Section -->
            <div class="form-section">
                <h2>Patient & Insurance Information</h2>

                <div class="form-row">
                    <div class="form-group">
                        <label for="txtPatientId">Patient ID:</label>
                        <asp:TextBox ID="txtPatientId" runat="server" CssClass="form-control" placeholder="Enter patient ID"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label for="txtInsuranceCompany">Insurance Company:</label>
                        <asp:TextBox ID="txtInsuranceCompany" runat="server" CssClass="form-control" placeholder="Enter insurance company"></asp:TextBox>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label for="txtPolicyBand">Policy Type:</label>
                        <asp:TextBox ID="txtPolicyBand" runat="server" CssClass="form-control" placeholder="Enter policy type"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label for="txtPolicyId">Policy ID:</label>
                        <asp:TextBox ID="txtPolicyId" runat="server" CssClass="form-control" placeholder="Enter policy ID"></asp:TextBox>
                    </div>
                </div>

                <h2>Clinician Information</h2>

                <div class="form-row">
                    <div class="form-group">
                        <label for="txtDoctorName">Clinician Name:</label>
                        <asp:TextBox ID="txtDoctorName" runat="server" CssClass="form-control" placeholder="Enter your name"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label for="txtDoctorSpecialization">Specialization:</label>
                        <asp:TextBox ID="txtDoctorSpecialization" runat="server" CssClass="form-control" placeholder="Enter your specialization"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label for="txtDoctorId">Clinician ID:</label>
                        <asp:TextBox ID="txtDoctorId" runat="server" CssClass="form-control" placeholder="Enter your ID"></asp:TextBox>
                    </div>
                </div>

                <h2>Clinical Information</h2>

                <div class="form-group">
                    <label for="txtPatientHistory">Patient History:</label>
                    
                    <asp:TextBox ID="txtPatientHistory" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="6" placeholder='[{"date": "MM/DD/YYYY", "doctor": "Dr. Name", "diagnosis": "Diagnosis", "treatment": "Treatment"}]'></asp:TextBox>
                </div>

                <div class="form-group">
                    <label for="txtDoctorNotes">Clinical Notes: <span class="required">*</span></label>
                    <asp:TextBox ID="txtDoctorNotes" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="8" placeholder="Enter your detailed clinical notes here"></asp:TextBox>
                </div>

                <div class="btn-group">
                    <asp:Button ID="btnReviewNotes" runat="server" Text="Review Clinical Notes" CssClass="btn btn-primary action-button" OnClick="btnReviewNotes_Click" />
                </div>
            </div>

            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                    <!-- Hidden field to track modal state -->
                    <asp:HiddenField ID="hdnShowModal" runat="server" Value="" />
                    <asp:HiddenField ID="hdnEnhancedNotesData" runat="server" />
                    <!-- Hidden field to store selections -->
                    <asp:HiddenField ID="hdnSelectedNotes" runat="server" />

                    <!-- Action Buttons Section (when review is complete) -->
                    <asp:Panel ID="pnlActionButtons" runat="server" CssClass="action-buttons" Visible="false">
                        <div class="btn-group">
                            <asp:Button ID="btnViewResults" runat="server" Text="View Review Results" CssClass="btn btn-primary action-button" OnClientClick="showReviewResultsModal(); return false;" />
                            <asp:Button ID="btnEnhanceNotes" runat="server" Text="Enhance Clinical Notes" CssClass="btn btn-primary action-button" OnClick="btnEnhanceNotes_Click" />
                            <asp:Button ID="btnGenerateClaim" runat="server" Text="Generate Insurance Claim" CssClass="btn btn-primary action-button" OnClick="btnGenerateClaim_Click" />
                        </div>
                    </asp:Panel>

                    <!-- Hidden Review Results Section (will be shown as popup) -->
                    <asp:Panel ID="pnlReviewResults" runat="server" CssClass="result-panel" Visible="false">
                        <div class="review-info">
                            <div class="review-explanation">
                                <p>Your clinical notes have been reviewed for completeness, accuracy, and insurance compatibility.</p>
                                <p>Please review the feedback below to ensure your documentation meets all standards and requirements.</p>
                            </div>
                            <asp:Label ID="lblStatus" runat="server" CssClass="status-info"></asp:Label>
                            <asp:Label ID="lblRequestId" runat="server" CssClass="request-info"></asp:Label>
                        </div>

                        <h3>Clinical Documentation Review</h3>
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

                    <!-- Hidden Enhanced Notes Section (will be shown as popup) -->
                    <asp:Panel ID="pnlEnhancedNotes" runat="server" CssClass="result-panel" Visible="false">
                        <div class="result-content">
                            <asp:Literal ID="litEnhancedNotes" runat="server"></asp:Literal>
                        </div>

                        <!-- Hidden button to process server-side -->
                        <asp:Button ID="btnServerApproveNotes" runat="server" Text="Approve Notes" CssClass="hidden-button" 
                            OnClick="btnServerApproveNotes_Click" Style="display: none;" />
                    </asp:Panel>

                    <!-- Hidden Generated Claim Section (will be shown as popup) -->
                    <asp:Panel ID="pnlGeneratedClaim" runat="server" CssClass="result-panel" Visible="false">
                        <div class="result-content">
                            <asp:Literal ID="litGeneratedClaim" runat="server"></asp:Literal>
                        </div>

                        <!-- Hidden field to store selections -->
                        <asp:HiddenField ID="hdnSelectedDiagnoses" runat="server" />

                        <!-- Hidden button to process server-side -->
                        <asp:Button ID="btnServerApproveDiagnoses" runat="server" Text="Approve Diagnoses" CssClass="hidden-button" OnClick="btnServerApproveDiagnoses_Click" Style="display: none;" />

                        <!-- Hidden button for generating claim from enhanced notes -->
                        <asp:Button ID="btnGenerateClaimFromNotes" runat="server" Text="Generate Claim" CssClass="hidden-button" OnClick="btnGenerateClaim_Click" Style="display: none;" />
                    </asp:Panel>

                    <!-- Final Notes Panel -->
                    <asp:Panel ID="pnlFinalNotes" runat="server" CssClass="result-panel" Visible="false">
                        <div class="panel-header">
                            <h2>Finalize Clinical Documentation</h2>
                            <div class="panel-description">
                                <p>Review and edit your clinical notes below before saving to the patient record.</p>
                                <p>These notes have been compiled from the sections you selected in the enhanced documentation.</p>
                            </div>
                        </div>

                        <div class="form-group">
                            <label for="txtFinalNotes">Final Clinical Notes:</label>
                            <div class="note-tools">
                                <button type="button" class="btn btn-sm btn-outline" onclick="insertTimestamp()">Insert Timestamp</button>
                                <button type="button" class="btn btn-sm btn-outline" onclick="insertSignature()">Insert Signature</button>
                            </div>
                            <asp:TextBox ID="txtFinalNotes" runat="server" CssClass="form-control final-notes-editor" TextMode="MultiLine" Rows="15"></asp:TextBox>
                        </div>

                        <div class="btn-group">
                            <asp:Button ID="btnSaveFinalNotes" runat="server" Text="Save & Submit Documentation" CssClass="btn btn-primary action-button" OnClick="btnSaveFinalNotes_Click" />
                            <asp:Button ID="btnEditFinalNotes" runat="server" Text="Continue Editing" CssClass="btn btn-secondary action-button" OnClick="btnEditFinalNotes_Click" />
                            <button type="button" id="btnBackToEnhanced" class="btn btn-outline action-button" onclick="goBackToEnhanced()">Back to Enhanced Notes</button>
                        </div>
                    </asp:Panel>
                    <script type="text/javascript">
                        function insertTimestamp() {
                            var txtArea = document.getElementById('<%=txtFinalNotes.ClientID%>');
                            var date = new Date();
                            var timestamp = date.toLocaleString();
                            var cursorPos = txtArea.selectionStart;
                            var textBefore = txtArea.value.substring(0, cursorPos);
                            var textAfter = txtArea.value.substring(cursorPos, txtArea.value.length);

                            txtArea.value = textBefore + "[" + timestamp + "] " + textAfter;
                            txtArea.focus();
                            txtArea.selectionStart = cursorPos + timestamp.length + 3;
                            txtArea.selectionEnd = cursorPos + timestamp.length + 3;
                        }

                        function insertSignature() {
                            var txtArea = document.getElementById('<%=txtFinalNotes.ClientID%>');
                            var doctorName = '<%=txtDoctorName.Text%>';
                            var signature = "\n\n--\nDocumented by: " + doctorName + ", " + '<%=txtDoctorSpecialization.Text%>' + "\nDate: " + new Date().toLocaleDateString() + "\n";
        
                            txtArea.value += signature;
                            txtArea.focus();
                            txtArea.scrollTop = txtArea.scrollHeight;
                        }
    
                        function goBackToEnhanced() {
                            // Store current text in case they want to come back to it
                            sessionStorage.setItem('finalNotesText', document.getElementById('<%=txtFinalNotes.ClientID%>').value);
        
                            // Show the enhanced notes modal again
                            window.showEnhancedNotesModal();
                        }
    
                        // Restore any saved text when page loads
                        window.addEventListener('load', function() {
                            var savedText = sessionStorage.getItem('finalNotesText');
                            if (savedText && document.getElementById('<%=txtFinalNotes.ClientID%>')) {
                                document.getElementById('<%=txtFinalNotes.ClientID%>').value = savedText;
                            }
                        });
                    </script>

                    <!-- Error Display -->
                    <asp:Panel ID="pnlError" runat="server" CssClass="error-panel" Visible="false">
                        <div class="error-header">
                            <span class="error-title">System Message</span>
                            <asp:LinkButton ID="btnCloseError" runat="server" CssClass="close-button" OnClick="btnCloseError_Click" Text="×"></asp:LinkButton>
                        </div>
                        <div class="error-content">
                            <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message"></asp:Label>

                            <asp:Panel ID="pnlJsonContent" runat="server" CssClass="json-content expanded" Visible="false">
                                <div class="json-header">
                                    <span>Technical Details</span>
                                    <asp:LinkButton ID="btnExpandCollapse" runat="server" CssClass="expand-collapse" OnClientClick="toggleJsonContent(); return false;">Collapse</asp:LinkButton>
                                </div>
                                <pre id="preFormattedJson" runat="server" class="formatted-json"></pre>
                            </asp:Panel>
                        </div>
                    </asp:Panel>

                    <!-- Confirmation Panel -->
                    <asp:Panel ID="pnlConfirmation" runat="server" CssClass="confirmation-panel" Visible="false">
                        <div class="confirmation-header">
                            <span class="confirmation-title">Success</span>
                            <asp:LinkButton ID="btnCloseConfirmation" runat="server" CssClass="close-button" OnClick="btnCloseConfirmation_Click" Text="×"></asp:LinkButton>
                        </div>
                        <div class="confirmation-content">
                            <div class="confirmation-icon">✓</div>
                            <asp:Label ID="lblConfirmationMessage" runat="server" CssClass="confirmation-message"></asp:Label>

                            <div class="confirmation-actions">
                                <asp:Button ID="btnConfirmationOk" runat="server" Text="OK" CssClass="btn btn-primary" OnClick="btnCloseConfirmation_Click" />
                            </div>
                        </div>
                    </asp:Panel>
                </ContentTemplate>
            </asp:UpdatePanel>
            <div id="validationErrorContainer" style="display: none;" class="error"></div>
        </div>

        <!-- Modal containers will be generated by JavaScript -->

        <script src="Content/js/claimkit.js"></script>
        <script src="Content/js/ClaimKitv1-Modal.js"></script>
        <script src="Content/js/enhanced-notes.js"></script>
    </form>

    <script type="text/javascript">
        // Failsafe to hide loading indicator if scripts fail
        window.onload = function() {
            var loadingIndicator = document.getElementById('loadingIndicator');
            if (loadingIndicator) {
                loadingIndicator.style.display = 'none';
            }
        
            // Add click event listener to all buttons that should hide the spinner
            //var buttons = document.querySelectorAll('.btn');
            //buttons.forEach(function(button) {
            //    button.addEventListener('click', function() {
            //        setTimeout(function() {
            //            var loadingIndicator = document.getElementById('loadingIndicator');
            //            if (loadingIndicator) {
            //                loadingIndicator.style.display = 'none';
            //            }
            //        }, 2000); // 2 second failsafe
            //    });
            //});
        };
    </script>
</body>
</html>
