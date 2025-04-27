<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ClaimKitv1.Default" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ClaimKitv1 - Medical Claims Processing</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="stylesheet" href="Content/css/styles.css" />
    <link rel="stylesheet" href="Content/css/modal-styles.css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="header">
            <div class="container">
                <h1>ClaimKitv1 - Medical Claims Processing</h1>
            </div>
        </div>
        
        <div class="container">
            <!-- Loading Indicator -->
            <div id="loadingIndicator" style="display:none;" class="loading-indicator">
                <div class="spinner"></div>
                <div>Processing... Please wait</div>
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
                        <label for="txtPolicyBand">Policy Band:</label>
                        <asp:TextBox ID="txtPolicyBand" runat="server" CssClass="form-control" placeholder="Enter policy band"></asp:TextBox>
                    </div>
                    
                    <div class="form-group">
                        <label for="txtPolicyId">Policy ID:</label>
                        <asp:TextBox ID="txtPolicyId" runat="server" CssClass="form-control" placeholder="Enter policy ID"></asp:TextBox>
                    </div>
                </div>
                
                <h2>Doctor Information</h2>
                
                <div class="form-row">
                    <div class="form-group">
                        <label for="txtDoctorName">Doctor Name:</label>
                        <asp:TextBox ID="txtDoctorName" runat="server" CssClass="form-control" placeholder="Enter doctor name"></asp:TextBox>
                    </div>
                    
                    <div class="form-group">
                        <label for="txtDoctorSpecialization">Specialization:</label>
                        <asp:TextBox ID="txtDoctorSpecialization" runat="server" CssClass="form-control" placeholder="Enter specialization"></asp:TextBox>
                    </div>
                    
                    <div class="form-group">
                        <label for="txtDoctorId">Doctor ID:</label>
                        <asp:TextBox ID="txtDoctorId" runat="server" CssClass="form-control" placeholder="Enter doctor ID"></asp:TextBox>
                    </div>
                </div>
                
                <h2>Medical Information</h2>

                <div class="form-group">
                    <label for="txtPatientHistory">Patient History (JSON format):</label>
                    <div class="input-tooltip" data-tooltip="Enter patient history in JSON format. Each entry should include date, doctor, diagnosis, and treatment.">
                        <span class="tooltip-icon">ⓘ</span>
                    </div>
                    <asp:TextBox ID="txtPatientHistory" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="6" placeholder='[{"date": "DD/MM/YYYY", "doctor": "Dr. Name", "diagnosis": "Diagnosis", "treatment": "Treatment"}]'></asp:TextBox>
                </div>
                
                <div class="form-group">
                    <label for="txtDoctorNotes">Doctor Notes: <span class="required">*</span></label>
                    <asp:TextBox ID="txtDoctorNotes" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="6" placeholder="Enter detailed doctor notes"></asp:TextBox>
                </div>

                <div class="btn-group">
                    <asp:Button ID="btnReviewNotes" runat="server" Text="Review Notes" CssClass="btn btn-primary action-button" OnClick="btnReviewNotes_Click" />
                </div>
            </div>
            
            <!-- Action Buttons Section (when review is complete) -->
            <asp:Panel ID="pnlActionButtons" runat="server" CssClass="action-buttons" Visible="false">
                <div class="btn-group">
                    <asp:Button ID="btnViewResults" runat="server" Text="View Results" CssClass="btn btn-primary action-button" OnClientClick="showReviewResultsModal(); return false;" />
                    <asp:Button ID="btnEnhanceNotes" runat="server" Text="Enhance Notes" CssClass="btn btn-primary action-button" OnClick="btnEnhanceNotes_Click" />
                    <asp:Button ID="btnGenerateClaim" runat="server" Text="Generate Claim" CssClass="btn btn-primary action-button" OnClick="btnGenerateClaim_Click" />
                </div>
            </asp:Panel>
            
            <!-- Hidden Review Results Section (will be shown as popup) -->
            <asp:Panel ID="pnlReviewResults" runat="server" CssClass="result-panel" Visible="false">
                <h2>Review Results</h2>
                
                <div class="review-info">
                    <asp:Label ID="lblStatus" runat="server" CssClass="status-info"></asp:Label>
                    <asp:Label ID="lblRequestId" runat="server" CssClass="request-info"></asp:Label>
                </div>
                
                <h3>Review Categories</h3>
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
                <h2>Enhanced Notes</h2>
                <div class="result-content">
                    <asp:Literal ID="litEnhancedNotes" runat="server"></asp:Literal>
                </div>
                
                <div class="btn-group">
                    <asp:Button ID="btnViewResultsFromNotes" runat="server" Text="View Results" CssClass="btn btn-secondary action-button" OnClientClick="showReviewResultsModal(); return false;" />
                    <asp:Button ID="btnGenerateClaimFromNotes" runat="server" Text="Generate Claim" CssClass="btn btn-primary action-button" OnClick="btnGenerateClaim_Click" />
                </div>
            </asp:Panel>
            
            <!-- Hidden Generated Claim Section (will be shown as popup) -->
            <asp:Panel ID="pnlGeneratedClaim" runat="server" CssClass="result-panel" Visible="false">
                <h2>Generated Claim</h2>
                <div class="result-content">
                    <asp:Literal ID="litGeneratedClaim" runat="server"></asp:Literal>
                </div>
                
                <div class="btn-group">
                    <asp:Button ID="btnViewResultsFromClaim" runat="server" Text="View Results" CssClass="btn btn-secondary action-button" OnClientClick="showReviewResultsModal(); return false;" />
                    <asp:Button ID="btnEnhanceNotesFromClaim" runat="server" Text="View Enhanced Notes" CssClass="btn btn-secondary action-button" OnClientClick="showEnhancedNotesModal(); return false;" />
                </div>
            </asp:Panel>

            <!-- Error Display -->
            <asp:Panel ID="pnlError" runat="server" CssClass="error-panel" Visible="false">
                <div class="error-header">
                    <span class="error-title">Response Information</span>
                    <asp:LinkButton ID="btnCloseError" runat="server" CssClass="close-button" OnClick="btnCloseError_Click" Text="×"></asp:LinkButton>
                </div>
                <div class="error-content">
                    <asp:Label ID="lblErrorMessage" runat="server" CssClass="error-message"></asp:Label>
        
                    <asp:Panel ID="pnlJsonContent" runat="server" CssClass="json-content expanded" Visible="false">
                        <div class="json-header">
                            <span>Response Details</span>
                            <asp:LinkButton ID="btnExpandCollapse" runat="server" CssClass="expand-collapse" OnClientClick="toggleJsonContent(); return false;">Collapse</asp:LinkButton>
                        </div>
                        <pre ID="preFormattedJson" runat="server" class="formatted-json"></pre>
                    </asp:Panel>
                </div>
            </asp:Panel>

            <!-- JSON Response Panel -->
            <asp:Panel ID="pnlJsonResponse" runat="server" CssClass="json-response-panel" Visible="false">
                <div class="response-header">
                    <div class="response-status">
                        <asp:Label ID="lblResponseStatus" runat="server" CssClass="status-label"></asp:Label>
                        <asp:Label ID="Label1" runat="server" CssClass="request-id"></asp:Label>
                    </div>
                    <asp:LinkButton ID="btnCloseResponse" runat="server" CssClass="close-button" OnClick="btnCloseResponse_Click" Text="×"></asp:LinkButton>
                </div>
                <div class="response-content">
                    <div class="response-overview">
                        <asp:Label ID="lblResponseMessage" runat="server"></asp:Label>
                    </div>
        
                    <div class="response-tabs">
                        <asp:LinkButton ID="btnTabReview" runat="server" CssClass="tab-button active" OnClientClick="showTab('review'); return false;">Formatted Review</asp:LinkButton>
                        <asp:LinkButton ID="btnTabRaw" runat="server" CssClass="tab-button" OnClientClick="showTab('raw'); return false;">Raw JSON</asp:LinkButton>
                    </div>
        
                    <div id="tab-review" class="tab-content active">
                        <asp:Panel ID="pnlFormattedReview" runat="server" Visible="true">
                            <div id="divFormattedReview" runat="server"></div>
                        </asp:Panel>
                    </div>
        
                    <div id="tab-raw" class="tab-content">
                        <asp:Panel ID="pnlRawJson" runat="server" Visible="false">
                            <pre class="json-code"><asp:Literal ID="litRawJson" runat="server"></asp:Literal></pre>
                        </asp:Panel>
                    </div>
                </div>
            </asp:Panel>

            <div id="validationErrorContainer" style="display:none;" class="error"></div>
        </div>
        
        <!-- Modal containers will be generated by JavaScript -->
        
        <script src="Content/js/ClaimKitv1.js"></script>
        <script src="Content/js/ClaimKitv1-Modal.js"></script>
    </form>
</body>
</html>