using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using ClaimKitv1.Models;
using ClaimKitv1.Services;
using ClaimKit_v1.Models.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ClaimKitv1
{
    public partial class Default : System.Web.UI.Page
    {
        // Dependency injection could be used here in a more advanced implementation
        private readonly IClaimKitApiService _apiService;
        private readonly LoggingService _logger;

        // Store the request ID for sequential workflows
        private string _requestId;

        // Store checkout time for generate claim
        private long _checkoutTime;

        public Default()
        {
            // Simple service initialization - could use IoC container in a more advanced implementation
            _apiService = new ClaimKitApiService();
            _logger = LoggingService.Instance;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Set default values from configuration
                txtPolicyId.Text = WebConfigurationManager.AppSettings["DefaultPolicyId"];
                txtPatientHistory.Text = WebConfigurationManager.AppSettings["DefaultPatientHistory"];

                // Log application access
                _logger.LogUserAction("Application Access", "User accessed the ClaimKit application");
            }
            else
            {
                // Handle postback state for showing modals
                string modalToShow = hdnShowModal.Value;
                if (!string.IsNullOrEmpty(modalToShow))
                {
                    // Schedule the modal to be shown after the UpdatePanel completes
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowModalAfterPostback",
                        $"setTimeout(function() {{ window.{modalToShow}(); }}, 300);", true);

                    // Clear the modal flag
                    hdnShowModal.Value = "";
                }
            }

            // Retrieve requestId from ViewState if available
            if (ViewState["RequestId"] != null)
            {
                _requestId = ViewState["RequestId"].ToString();
            }
        }

        #region Button Click Event Handlers

        protected void btnReviewNotes_Click(object sender, EventArgs e)
        {
            // Hide previous results
            ResetResultPanels();

            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtDoctorNotes.Text))
            {
                DisplayError("Clinical notes are required to proceed with the review.");
                return;
            }

            try
            {
                // Log the action
                _logger.LogUserAction("Review Notes Initiated", $"Doctor: {txtDoctorName.Text}, Patient ID: {txtPatientId.Text}");

                // Register async task
                RegisterAsyncTask(new PageAsyncTask(PerformReviewAsync));

                // Show loading indicator (UI feedback)
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowLoading",
                    "document.getElementById('loadingIndicator').style.display = 'block';", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initiating review", ex);
                DisplayError("There was an issue starting the review process. Please try again.");
            }
        }

        protected void btnEnhanceNotes_Click(object sender, EventArgs e)
        {
            // Hide previous results
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;

            try
            {
                // Log the action
                _logger.LogUserAction("Enhance Notes Initiated", $"Request ID: {_requestId}");

                // Register async task
                RegisterAsyncTask(new PageAsyncTask(PerformEnhanceAsync));

                // Show loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowLoading",
                    "document.getElementById('loadingIndicator').style.display = 'block';", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initiating enhance notes", ex);
                DisplayError("There was an issue starting the enhancement process. Please try again.");
            }
        }

        protected void btnGenerateClaim_Click(object sender, EventArgs e)
        {
            // Hide previous results
            pnlGeneratedClaim.Visible = false;

            try
            {
                // Log the action
                _logger.LogUserAction("Generate Claim Initiated", $"Request ID: {_requestId}");

                // Register async task
                RegisterAsyncTask(new PageAsyncTask(PerformGenerateClaimAsync));

                // Show loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowLoading",
                    "document.getElementById('loadingIndicator').style.display = 'block';", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initiating claim generation", ex);
                DisplayError("There was an issue starting the claim generation process. Please try again.");
            }
        }

        protected void btnServerApproveNotes_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the selected notes from the hidden field
                string selectedNotesJson = hdnSelectedNotes.Value;
                if (string.IsNullOrEmpty(selectedNotesJson))
                {
                    DisplayError("No enhanced notes were selected. Please select at least one note to approve.");
                    return;
                }

                // Parse the selected notes
                var selectedNotes = JsonConvert.DeserializeObject<List<string>>(selectedNotesJson);

                // Log the action
                _logger.LogUserAction("Approved Enhanced Notes",
                    $"Request ID: {_requestId}, Notes Count: {selectedNotes.Count}");

                // Prepare final notes for editing
                PrepareAndShowFinalNotes(selectedNotes);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error approving notes", ex);
                DisplayError("There was an issue approving the enhanced notes. Please try again.");
            }
        }

        protected void btnServerApproveDiagnoses_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the selected diagnoses from the hidden field
                string selectedDiagnosesJson = hdnSelectedDiagnoses.Value;
                if (string.IsNullOrEmpty(selectedDiagnosesJson))
                {
                    DisplayError("No diagnoses were selected. Please select at least one diagnosis to approve.");
                    return;
                }

                // Parse the selected diagnoses
                var selectedDiagnoses = JsonConvert.DeserializeObject<List<string>>(selectedDiagnosesJson);

                // Log the action
                _logger.LogUserAction("Approved Claim Diagnoses",
                    $"Request ID: {_requestId}, Diagnoses Count: {selectedDiagnoses.Count}");

                // Show confirmation
                ShowConfirmation("The selected diagnoses have been approved and will be used for the insurance claim.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error approving diagnoses", ex);
                DisplayError("There was an issue approving the diagnoses. Please try again.");
            }
        }

        protected void btnSaveFinalNotes_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFinalNotes.Text))
                {
                    DisplayError("Final clinical notes cannot be empty. Please enter your notes.");
                    return;
                }

                // Log the action
                _logger.LogUserAction("Final Notes Saved", $"Request ID: {_requestId}");

                // In a real application, we would save the notes to a database here
                // For now, just show a confirmation message
                ShowConfirmation("Your clinical notes have been successfully saved and the claim information has been sent to the insurance company.");

                // Reset the form for a new entry
                ResetAllPanels();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving final notes", ex);
                DisplayError("There was an issue saving your final notes. Please try again.");
            }
        }

        protected void btnEditFinalNotes_Click(object sender, EventArgs e)
        {
            // Just keep the panel visible for further editing
            pnlFinalNotes.Visible = true;
        }

        protected void btnCloseError_Click(object sender, EventArgs e)
        {
            pnlError.Visible = false;
        }

        protected void btnCloseConfirmation_Click(object sender, EventArgs e)
        {
            pnlConfirmation.Visible = false;
        }

        #endregion

        #region Async API Call Methods

        private async Task PerformReviewAsync()
        {
            try
            {
                // Set check-in time to current time
                long currentTimestamp = GetCurrentUnixTimestamp();

                // Store checkout time for later use (checkout time is check-in time + 1 hour for this example)
                _checkoutTime = currentTimestamp + 3600; // Add 1 hour
                ViewState["CheckoutTime"] = _checkoutTime;

                // Parse patient history (validate JSON format)
                JArray patientHistory = ParsePatientHistory();
                if (patientHistory == null) return;

                // Create review request
                var reviewRequest = new ReviewRequest
                {
                    HospitalId = ConfigurationService.HospitalId,
                    ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                    HospitalPatientId = txtPatientId.Text,
                    DoctorNotes = txtDoctorNotes.Text,
                    InsuranceCompany = txtInsuranceCompany.Text,
                    PolicyBand = txtPolicyBand.Text,
                    PolicyId = txtPolicyId.Text,
                    PatientCheckinTime = currentTimestamp,
                    DoctorName = txtDoctorName.Text,
                    DoctorSpecialization = txtDoctorSpecialization.Text,
                    HospitalDoctorId = txtDoctorId.Text,
                    PatientHistory = patientHistory
                };

                // Log the request details
                _logger.LogUserAction("Sending Review Request",
                    $"Patient ID: {reviewRequest.HospitalPatientId}, Insurance: {reviewRequest.InsuranceCompany}");

                // Call API service
                var response = await _apiService.ReviewNotesAsync(reviewRequest);

                // Process response
                ProcessReviewResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during review process", ex);
                DisplayError("There was an issue reviewing your clinical notes. Our technical team has been notified.");
            }
            finally
            {
                // Hide loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "HideLoading",
                    "document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
        }

        private async Task PerformEnhanceAsync()
        {
            try
            {
                // Check request ID
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("Please review your clinical notes first before enhancing them.");
                    return;
                }

                // Create enhance request with proper parameters
                var enhanceRequest = new EnhanceRequest
                {
                    HospitalId = ConfigurationService.HospitalId,
                    ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                    RequestId = _requestId
                };

                // Clear debug log and add request
                _logger.LogUserAction("Enhance Request", JsonConvert.SerializeObject(enhanceRequest));

                // Call API service
                var response = await _apiService.EnhanceNotesAsync(enhanceRequest);

                // Log raw response for debugging
                _logger.LogUserAction("Enhance Response", response.RawResponse);

                // Process response
                ProcessEnhanceResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Enhancement Error", ex);
                DisplayError("There was an issue enhancing your clinical notes. Please try again.");
            }
            finally
            {
                // Hide loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "HideLoading",
                    "document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
        }

        private async Task PerformGenerateClaimAsync()
        {
            try
            {
                // Check request ID
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("Please review your clinical notes first before generating a claim.");
                    return;
                }

                // Get checkout time from ViewState
                long checkoutTime;
                if (ViewState["CheckoutTime"] != null)
                {
                    checkoutTime = (long)ViewState["CheckoutTime"];
                }
                else
                {
                    // Default to current time + 1 hour if missing
                    checkoutTime = GetCurrentUnixTimestamp() + 3600;
                }

                // Create generate claim request
                var generateClaimRequest = new GenerateClaimRequest
                {
                    HospitalId = ConfigurationService.HospitalId,
                    ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                    RequestId = _requestId,
                    PatientCheckoutTime = checkoutTime,
                    HospitalPatientId = txtPatientId.Text,
                    InsuranceCompany = txtInsuranceCompany.Text,
                    PolicyBand = txtPolicyBand.Text,
                    PolicyId = txtPolicyId.Text
                };

                // Log request for debugging
                _logger.LogUserAction("Claim Request", JsonConvert.SerializeObject(generateClaimRequest));

                // Call API service
                var response = await _apiService.GenerateClaimAsync(generateClaimRequest);

                // Log raw response
                _logger.LogUserAction("Claim Response", response.RawResponse);

                // Process response
                ProcessGenerateClaimResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Claim Generation Error", ex);
                DisplayError("There was an issue generating the insurance claim. Please try again.");
            }
            finally
            {
                // Hide loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "HideLoading",
                    "document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
        }

        #endregion

        #region Response Processing Methods

        private void ProcessReviewResponse(ReviewResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the medical records system.");
                return;
            }

            // Log the response
            _logger.LogUserAction("Review Response Received",
                $"Success: {response.IsSuccess}, Message: {response.Message}");

            if (response.IsSuccess)
            {
                // Store request ID for future calls
                _requestId = response.RequestId;
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("Request ID not returned from the medical records system.");
                    return;
                }

                ViewState["RequestId"] = _requestId;

                // Display success message
                lblStatus.Text = $"<div class='success'>Status: {FormatStatusMessage(response.Message)}</div>";
                lblRequestId.Text = $"<div>Request ID: {_requestId}</div>";

                // Bind review categories to repeater if available
                if (response.Review != null && response.Review.Count > 0)
                {
                    rptReviewCategories.DataSource = response.Review;
                    rptReviewCategories.DataBind();
                }
                else
                {
                    // No review data available
                    lblStatus.Text += "<div>No clinical review data available</div>";
                }

                // Show results panel and enable enhance/generate buttons
                pnlReviewResults.Visible = true;
                pnlActionButtons.Visible = true;

                // Show the review results modal
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowReviewResults",
                    "window.showReviewResultsModal();", true);

                //// Hide loading indicator at the start of processing the response
                //ScriptManager.RegisterStartupScript(this, GetType(), "HideLoadingFirst",
                //    "if(document.getElementById('loadingIndicator')) document.getElementById('loadingIndicator').style.display = 'none';", true);

            }
            else
            {
                // Display error message with a more user-friendly format
                DisplayError($"The clinical notes review could not be completed: {FormatErrorMessage(response.Message)}");
            }
        }

        private void ProcessEnhanceResponse(EnhanceResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the system.");
                return;
            }

            if (response.IsSuccess)
            {
                if (response.Data != null && response.Data.EnhancedNotes != null)
                {
                    // Display enhanced notes
                    litEnhancedNotes.Text = JsonConvert.SerializeObject(response.Data.EnhancedNotes, Formatting.Indented);
                    pnlEnhancedNotes.Visible = true;

                    // Set the modal to show after UpdatePanel refresh
                    hdnShowModal.Value = "showEnhancedNotesModal";
                }
                else
                {
                    DisplayError("No enhanced clinical notes were found in the response.");
                }
            }
            else
            {
                // Display error message
                DisplayError($"The clinical notes enhancement could not be completed: {FormatErrorMessage(response.Message)}");
            }
        }

        private void ProcessGenerateClaimResponse(GenerateClaimResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the system.");
                return;
            }

            if (response.IsSuccess)
            {
                if (response.Data != null && response.Data.GeneratedClaim != null)
                {
                    // Display generated claim
                    litGeneratedClaim.Text = JsonConvert.SerializeObject(response.Data.GeneratedClaim, Formatting.Indented);
                    pnlGeneratedClaim.Visible = true;

                    // Set the modal to show after UpdatePanel refresh
                    hdnShowModal.Value = "showGeneratedClaimModal";
                }
                else
                {
                    DisplayError("No insurance claim data was found in the response.");
                }
            }
            else
            {
                // Display error message
                DisplayError($"The insurance claim generation could not be completed: {FormatErrorMessage(response.Message)}");
            }
        }

        #endregion

        #region Repeater Event Handlers

        protected void rptReviewCategories_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                // Get the literal control
                var litCategoryContent = (Literal)e.Item.FindControl("litCategoryContent");
                if (litCategoryContent == null) return;

                // Get the data item
                var dataItem = e.Item.DataItem;

                // Build the HTML content
                var content = new StringBuilder();

                try
                {
                    if (dataItem is ReviewCategory category)
                    {
                        // Format category name for display
                        string displayName = FormatCategoryName(category.Category ?? "Unknown Category");

                        // Determine status class
                        string statusClass = GetStatusClass(category.Status);

                        // Create formatted HTML with category header and content
                        content.AppendLine($"<div class=\"category-header\">");
                        content.AppendLine($"  <span class=\"category-title\">{displayName}</span>");
                        content.AppendLine($"  <span class=\"expand-icon\">▼</span>");
                        content.AppendLine($"</div>");
                        content.AppendLine($"<div class=\"category-content\">");

                        if (!string.IsNullOrEmpty(category.Status))
                        {
                            content.AppendLine($"  <div class=\"status-badge {statusClass}\">{category.Status}</div>");
                        }

                        if (!string.IsNullOrEmpty(category.Reason))
                        {
                            content.AppendLine($"  <div class=\"section-reasoning\">{category.Reason}</div>");
                        }

                        content.AppendLine($"</div>");
                    }
                    else
                    {
                        // Fallback if it's not a ReviewCategory
                        content.AppendLine("<div class=\"category-header\">");
                        content.AppendLine("  <span class=\"category-title\">Review Information</span>");
                        content.AppendLine("  <span class=\"expand-icon\">▼</span>");
                        content.AppendLine("</div>");
                        content.AppendLine("<div class=\"category-content\">");
                        content.AppendLine("  <div class=\"section-reasoning\">The review format was not recognized. Please contact support.</div>");
                        content.AppendLine("</div>");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't expose to user
                    _logger.LogError("Error formatting review category", ex);

                    content.Clear();
                    content.AppendLine("<div class=\"category-header\">");
                    content.AppendLine("  <span class=\"category-title\">Review Information</span>");
                    content.AppendLine("  <span class=\"expand-icon\">▼</span>");
                    content.AppendLine("</div>");
                    content.AppendLine("<div class=\"category-content\">");
                    content.AppendLine("  <div class=\"section-reasoning\">There was an issue displaying this review category.</div>");
                    content.AppendLine("</div>");
                }

                // Set the content
                litCategoryContent.Text = content.ToString();
            }
        }

        #endregion

        #region Helper Methods

        private void ResetResultPanels()
        {
            pnlReviewResults.Visible = false;
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;
            pnlFinalNotes.Visible = false;
            pnlError.Visible = false;
            pnlConfirmation.Visible = false;
        }

        private void ResetAllPanels()
        {
            ResetResultPanels();
            pnlActionButtons.Visible = false;

            // Clear input fields
            txtDoctorNotes.Text = string.Empty;
            txtFinalNotes.Text = string.Empty;

            // Clear request ID
            _requestId = null;
            ViewState["RequestId"] = null;

            // Show success message
            ShowConfirmation("All clinical documentation has been completed successfully.");
        }

        private void PrepareAndShowFinalNotes(List<string> selectedNotes)
        {
            try
            {
                // Combine selected notes into a single document
                StringBuilder finalNotes = new StringBuilder();
                foreach (var note in selectedNotes)
                {
                    // Try to parse note identifier
                    string[] parts = note.Split('-');
                    if (parts.Length >= 2)
                    {
                        // Handle section-specific notes if applicable
                        if (parts.Length > 2)
                        {
                            string section = parts[1];
                            finalNotes.AppendLine($"=== {FormatSectionTitle(section)} ===");
                        }

                        // Look up the actual note content - this is simplified here
                        // In a real implementation, we would parse the original JSON to extract actual notes
                        finalNotes.AppendLine($"Enhanced clinical note approved by Dr. {txtDoctorName.Text}");
                        finalNotes.AppendLine();
                    }
                }

                // Set the final notes text
                txtFinalNotes.Text = finalNotes.ToString();

                // Show the panel for final editing
                pnlFinalNotes.Visible = true;

                // Log the action
                _logger.LogUserAction("Final Notes Prepared", $"Request ID: {_requestId}, Notes Count: {selectedNotes.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error preparing final notes", ex);
                DisplayError("There was an issue preparing the final clinical notes. Please try again.");
            }
        }

        private void ShowConfirmation(string message)
        {
            lblConfirmationMessage.Text = message;
            pnlConfirmation.Visible = true;

            // Register script to show confirmation visually
            ScriptManager.RegisterStartupScript(this, GetType(), "ShowConfirmation",
                "$('.confirmation-panel').addClass('visible');", true);
        }

        private void DisplayError(string errorMessage)
        {
            // Log the error
            _logger.LogUserAction("UI Error Displayed", errorMessage);

            // Set the basic error message, removing any debug prefixes
            lblErrorMessage.Text = CleanupErrorMessage(errorMessage);

            // Reset JSON panel
            pnlJsonContent.Visible = false;
            preFormattedJson.InnerHtml = string.Empty;

            // Try to extract and format JSON content
            string jsonContent = ExtractJsonContent(errorMessage);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                // Format JSON and show the panel
                pnlJsonContent.Visible = true;

                // Format JSON
                preFormattedJson.InnerHtml = FormatJsonToHtml(jsonContent);
            }

            // Show the error panel
            pnlError.Visible = true;
        }

        private string FormatStatusMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Review completed";

            // Replace underscores with spaces and title case
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                message.Replace('_', ' '));
        }

        private string FormatErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "An unknown error occurred";

            // Convert technical errors to user-friendly messages
            if (message.Contains("timeout") || message.Contains("timed out"))
                return "The system is taking longer than expected to respond. Please try again.";

            if (message.Contains("connection") || message.Contains("network"))
                return "Unable to connect to the medical records system. Please check your network connection.";

            if (message.Contains("authentication") || message.Contains("auth") || message.Contains("token"))
                return "Your session may have expired. Please refresh the page and try again.";

            // Default: clean up the message and make it more user-friendly
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                message.Replace('_', ' '));
        }

        private string FormatCategoryName(string categoryName)
        {
            // Remove numbering prefix if present (e.g., "1_history_diagnostic_analysis")
            if (categoryName.Contains("_") && char.IsDigit(categoryName[0]))
            {
                categoryName = categoryName.Substring(categoryName.IndexOf('_') + 1);
            }

            // Replace underscores with spaces
            categoryName = categoryName.Replace('_', ' ');

            // Title case the name
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryName);
        }

        private string FormatSectionTitle(string section)
        {
            if (string.IsNullOrEmpty(section))
                return string.Empty;

            // Replace underscores with spaces
            string result = section.Replace('_', ' ');

            // Add spaces before capital letters (for camelCase)
            result = Regex.Replace(result, "([A-Z])", " $1");

            // Capitalize first letter
            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1);
            }

            // Trim excess whitespace
            return result.Trim();
        }

        private string GetStatusClass(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "status-neutral";

            string lowerStatus = status.ToLower();

            if (lowerStatus.Contains("consistent") ||
                lowerStatus.Contains("complete") ||
                lowerStatus.Contains("compliant") ||
                lowerStatus.Contains("valid"))
                return "status-consistent";

            if (lowerStatus.Contains("inconsistent") ||
                lowerStatus.Contains("incomplete") ||
                lowerStatus.Contains("non-compliant") ||
                lowerStatus.Contains("invalid"))
                return "status-inconsistent";

            return "status-neutral";
        }

        private long GetCurrentUnixTimestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private JArray ParsePatientHistory()
        {
            if (string.IsNullOrWhiteSpace(txtPatientHistory.Text))
            {
                // Empty patient history is valid - return an empty array
                return new JArray();
            }

            try
            {
                return JArray.Parse(txtPatientHistory.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError("Patient history parsing error", ex, txtPatientHistory.Text);
                DisplayError("The patient history must be in a valid format. Please check your entry and try again.");
                return null;
            }
        }

        #region JSON Formatting Helper Methods

        private string CleanupErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            // Remove common debug prefixes
            message = message.Replace("Debug - Raw API Response:", "").Trim();
            message = message.Replace("API Response:", "").Trim();

            // Try to remove any JSON content
            string jsonContent = ExtractJsonContent(message);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                message = message.Replace(jsonContent, "").Trim();
            }

            // Clean up any multiple spaces or line breaks
            message = Regex.Replace(message, @"\s+", " ").Trim();

            if (string.IsNullOrEmpty(message))
            {
                return "Response received from server";
            }

            return message;
        }

        private string ExtractJsonContent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                // Try to parse the entire text as JSON first
                JToken.Parse(text);
                return text; // The entire text is valid JSON
            }
            catch
            {
                // If that fails, try to extract JSON objects or arrays
                try
                {
                    // Look for JSON objects
                    Match objectMatch = Regex.Match(text, @"(\{[\s\S]*\})", RegexOptions.Singleline);
                    if (objectMatch.Success)
                    {
                        string jsonObj = objectMatch.Groups[1].Value;
                        JToken.Parse(jsonObj); // Validate it's valid JSON
                        return jsonObj;
                    }
                }
                catch
                {
                    // Not a valid JSON object
                }

                try
                {
                    // Look for JSON arrays
                    Match arrayMatch = Regex.Match(text, @"(\[[\s\S]*\])", RegexOptions.Singleline);
                    if (arrayMatch.Success)
                    {
                        string jsonArray = arrayMatch.Groups[1].Value;
                        JToken.Parse(jsonArray); // Validate it's valid JSON
                        return jsonArray;
                    }
                }
                catch
                {
                    // Not a valid JSON array
                }
            }

            return null; // No valid JSON found
        }

        private string FormatJsonToHtml(string jsonString)
        {
            try
            {
                // Parse and format the JSON with indentation
                var parsedJson = JToken.Parse(jsonString);
                string formattedJson = parsedJson.ToString(Formatting.Indented);

                // Escape HTML entities to prevent XSS
                formattedJson = System.Web.HttpUtility.HtmlEncode(formattedJson);

                // Apply syntax highlighting with HTML/CSS
                formattedJson = ApplySyntaxHighlighting(formattedJson);

                return formattedJson;
            }
            catch (Exception ex)
            {
                // Return the original string if parsing fails
                _logger.LogError("Error formatting JSON", ex);
                return "Error formatting JSON: " + ex.Message + "<br><br>" +
                       System.Web.HttpUtility.HtmlEncode(jsonString);
            }
        }

        /// <summary>
        /// Applies syntax highlighting to formatted JSON
        /// </summary>
        private string ApplySyntaxHighlighting(string jsonString)
        {
            // Highlight keys (anything followed by a colon)
            jsonString = Regex.Replace(
                jsonString,
                "(&quot;[^&]*?&quot;):",
                "<span class=\"json-key\">$1</span>:"
            );

            // Highlight string values (anything in quotes that's not followed by a colon)
            jsonString = Regex.Replace(
                jsonString,
                "(?<=: )(&quot;.*?&quot;)(?=,|\\n|\\r|$)",
                "<span class=\"json-string\">$1</span>"
            );

            // Highlight numbers
            jsonString = Regex.Replace(
                jsonString,
                "(?<=: )(\\d+\\.?\\d*)(?=,|\\n|\\r|$)",
                "<span class=\"json-number\">$1</span>"
            );

            // Highlight booleans
            jsonString = Regex.Replace(
                jsonString,
                "(?<=: )(true|false)(?=,|\\n|\\r|$)",
                "<span class=\"json-boolean\">$1</span>"
            );

            // Highlight null values
            jsonString = Regex.Replace(
                jsonString,
                "(?<=: )(null)(?=,|\\n|\\r|$)",
                "<span class=\"json-null\">$1</span>"
            );

            // Convert newlines to HTML line breaks for proper display
            jsonString = jsonString.Replace("\n", "<br>");

            // Add non-breaking spaces for indentation (preserve formatting)
            jsonString = jsonString.Replace("  ", "&nbsp;&nbsp;");

            return jsonString;
        }

        #endregion

        #endregion
    }
}