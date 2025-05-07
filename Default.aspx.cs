using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using ClaimKitv1.Models;
using ClaimKitv1.Services;
using ClaimKitv1.Models.Responses;
using ClaimKitv1.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using System.IO;

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
                    if(modalToShow == "showGeneratedClaimModal")
                    // Schedule the modal to be shown after the UpdatePanel completes
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowModalAfterPostback",
                        $"setTimeout(function() {{ window.{modalToShow}(); }}, 300);", true);

                    // Clear the modal flag
                    hdnShowModal.Value = "";
                }
            }
            // Always handle modal logic safely
            //string modalToShow = hdnShowModal.Value;
            //if (!string.IsNullOrEmpty(modalToShow))
            //{
            //    // Only show modal if we are NOT coming from btnServerApproveNotes
            //    // (which already handles modal logic and shouldn't reopen anything)
            //    if (!IsPostBack || Request.Form["__EVENTTARGET"] != btnServerApproveNotes.UniqueID)
            //    {
            //        ScriptManager.RegisterStartupScript(this, GetType(), "ShowModalAfterPostback",
            //            $"setTimeout(function() {{ window.{modalToShow}(); }}, 300);", true);
            //    }

            //    // Clear the modal trigger regardless
            //    hdnShowModal.Value = "";
            //}

            // Retrieve requestId from ViewState if available
            if (ViewState["RequestId"] != null)
            {
                _requestId = ViewState["RequestId"].ToString();
            }

            // Retrieve enhanced notes data from ViewState if available
            if (ViewState["EnhancedNotesData"] != null && hdnEnhancedNotesData != null)
            {
                hdnEnhancedNotesData.Value = ViewState["EnhancedNotesData"].ToString();
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
                DateTime startTime = DateTime.Now;

                // Log the action
                _logger.LogUserAction("Review / Enhance Notes Initiated", $"Doctor: {txtDoctorName.Text}, Patient ID: {txtPatientId.Text}, Start Time: {FormatResponseTime(TimeSpan.FromTicks(startTime.Ticks))}");

                // Register async task
                RegisterAsyncTask(new PageAsyncTask(PerformReviewAndEnhanceAsync));

                DateTime endTime = DateTime.Now;
                TimeSpan responseTime = endTime - startTime;

                _logger.LogUserAction("Review / Enhance Notes Ended", $"Patient ID: {txtPatientId.Text}, End Time: {FormatResponseTime(TimeSpan.FromTicks(endTime.Ticks))}, Response Time: {FormatResponseTime(responseTime)}");

                // Show loading indicator (UI feedback)
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowLoading",
                    "document.getElementById('loadingIndicator').style.display = 'block';", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initiating review / enhance", ex);
                DisplayError("There was an issue starting the review and enhancement process. Please try again.");
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

                // Ensure modal init is always triggered
                ScriptManager.RegisterStartupScript(this, GetType(), "InitEnhancedModal",
                    "if(typeof window.initEnhancedNotesModal === 'function') { window.initEnhancedNotesModal(); } else { window.setTimeout(function() { if(window.showEnhancedNotesModal) window.showEnhancedNotesModal(); }, 1000); }", true);
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

                // Log for debugging
                _logger.LogUserAction("Selected Notes JSON", selectedNotesJson);

                // Default selections handling
                List<string> selectedSections = new List<string>();

                if (string.IsNullOrEmpty(selectedNotesJson) || selectedNotesJson == "[]")
                {
                    // If nothing is selected, try to find the sections from the stored data
                    string enhancedDataJson = hdnEnhancedNotesData.Value;

                    if (!string.IsNullOrEmpty(enhancedDataJson))
                    {
                        var enhancedData = JObject.Parse(enhancedDataJson);

                        if (enhancedData["sections"] != null && enhancedData["sections"].Type == JTokenType.Object)
                        {
                            // Get all section keys
                            foreach (var prop in ((JObject)enhancedData["sections"]).Properties())
                            {
                                selectedSections.Add(prop.Name);
                            }

                            _logger.LogUserAction("Auto-selected all sections",
                                $"Count: {selectedSections.Count}, Section names: {string.Join(", ", selectedSections)}");
                        }
                    }

                    // If we still have no selections, show error
                    if (selectedSections.Count == 0)
                    {
                        DisplayError("No sections were selected. Please select at least one section to approve.");
                        return;
                    }
                }
                else
                {
                    // Parse the selected notes from JSON
                    try
                    {
                        selectedSections = JsonConvert.DeserializeObject<List<string>>(selectedNotesJson);

                        if (selectedSections == null || selectedSections.Count == 0)
                        {
                            DisplayError("No sections were selected. Please select at least one section to approve.");
                            return;
                        }

                        _logger.LogUserAction("Parsed Selected Sections",
                            $"Count: {selectedSections.Count}, Section names: {string.Join(", ", selectedSections)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error parsing selected sections", ex, selectedNotesJson);
                        DisplayError("There was an error processing your selections. Please try again.");
                        return;
                    }
                }

                // Store selected sections in ViewState for potential back navigation
                ViewState["SelectedSections"] = selectedNotesJson;

                // Check if we're coming back from final notes (via "Back to Enhanced Notes" button)
                if (Session["SavedFinalNotes"] != null)
                {
                    // Restore the previously edited final notes
                    string savedNotes = Session["SavedFinalNotes"].ToString();

                    // Clear the session variable after use
                    Session.Remove("SavedFinalNotes");

                    // Prepare final notes with the saved text
                    txtFinalNotes.Text = savedNotes;

                    _logger.LogUserAction("Restored Final Notes",
                        $"Request ID: {_requestId}, Length: {savedNotes.Length} characters");
                }
                else
                {
                    // Normal flow - prepare final notes with the selected sections
                    PrepareAndShowFinalNotes(selectedSections);
                }

                // Show the final notes panel
                pnlFinalNotes.Visible = true;

                // Hide other panels
                pnlEnhancedNotes.Visible = false;

                // Log the action
                _logger.LogUserAction("Approved Enhanced Notes",
                    $"Request ID: {_requestId}, Section Count: {selectedSections.Count}");

                hdnShowModal.Value = "";
            }
            catch (Exception ex)
            {
                _logger.LogError("Error approving notes", ex);
                DisplayError("There was an issue approving the enhanced notes. Please try again.");
            }
            finally
            {
                // Hide loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "HideLoading",
                    "document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
        }

        protected void btnBackToEnhanced_Click(object sender, EventArgs e)
        {
            try
            {
                // Save current final notes to session state
                if (!string.IsNullOrEmpty(txtFinalNotes.Text))
                {
                    Session["SavedFinalNotes"] = txtFinalNotes.Text;
                }

                // Show enhanced notes again
                ShowEnhancedNotesModal();

                _logger.LogUserAction("Returned to Enhanced Notes", $"Request ID: {_requestId}");                
            }
            catch (Exception ex)
            {
                _logger.LogError("Error returning to enhanced notes", ex);
                DisplayError("There was an issue returning to the enhanced notes view. Please try again.");
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

                // 1. Save the final notes to a text file
                string savedNotesDir = Path.Combine(Server.MapPath("~/"), "Saved Notes");

                // Create directory if it doesn't exist
                if (!Directory.Exists(savedNotesDir))
                {
                    Directory.CreateDirectory(savedNotesDir);
                }

                // Generate filename with timestamp and patient ID
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string patientId = string.IsNullOrEmpty(txtPatientId.Text) ? "unknown" : txtPatientId.Text;
                string doctorId = string.IsNullOrEmpty(txtDoctorId.Text) ? "unknown" : txtDoctorId.Text;
                string filename = $"Notes_{patientId}_{doctorId}_{timestamp}.txt";
                string filePath = Path.Combine(savedNotesDir, filename);

                // Write the final notes to the file
                File.WriteAllText(filePath, txtFinalNotes.Text);

                // 2. Update the original clinical notes with the final content
                txtDoctorNotes.Text = txtFinalNotes.Text;

                // 3. Log the action
                _logger.LogUserAction("Final Notes Saved",
                    $"Request ID: {_requestId}, File: {filename}, Patient ID: {patientId}");

                // 4. Show confirmation to user
                ShowConfirmation($"Your clinical notes have been successfully saved as {filename} and the claim information has been sent to the insurance company.");

                // 5. Reset the workflow panels but keep the doctor notes populated
                ResetWorkflowPanels();

                // Reset the form for a new entry
                //ResetAllPanels();
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

        private async Task PerformReviewAndEnhanceAsync()
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

                DateTime startTime = DateTime.Now;

                // Call ReviewNotesAsync API service
                var reviewResponse = await _apiService.ReviewNotesAsync(reviewRequest);
                DateTime endTime = DateTime.Now;
                reviewResponse.ResponseTime = endTime - startTime;

                // Process review response
                ProcessReviewResponse(reviewResponse);

                //If review was successful, automatically enhance notes
                if (reviewResponse.IsSuccess && !string.IsNullOrEmpty(reviewResponse.RequestId))
                {
                    _requestId = reviewResponse.RequestId;
                    ViewState["RequestId"] = _requestId;

                    // Log the enhancement action
                    _logger.LogUserAction("Auto-Enhancement Starting", $"Request ID: {_requestId}");

                    // Create enhance request with proper parameters
                    var enhanceRequest = new EnhanceRequest
                    {
                        HospitalId = ConfigurationService.HospitalId,
                        ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                        RequestId = _requestId
                    };

                    startTime = DateTime.Now;
                    // Call EnhanceNotesAsync API service for enhancement
                    var enhanceResponse = await _apiService.EnhanceNotesAsync(enhanceRequest);
                    endTime = DateTime.Now;
                    enhanceResponse.ResponseTime = endTime - startTime;
                    // Process enhance response
                    ProcessEnhanceResponse(enhanceResponse);

                    // Log the combined operation completion
                    _logger.LogUserAction("Review and Enhance Completed",
                        $"Request ID: {_requestId}, Review Status: {reviewResponse.Status}, Enhance Status: {enhanceResponse.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during review and enhance process", ex);
                DisplayError("There was an issue reviewing and enhancing your clinical notes. Our technical team has been notified.");
            }
            finally
            {
                // Hide loading indicator
                ScriptManager.RegisterStartupScript(this, GetType(), "HideLoading",
                    "document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
        }

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

                DateTime startTime = DateTime.Now;
                // Call ReviewNotesAsync API service
                var reviewResponse = await _apiService.ReviewNotesAsync(reviewRequest);
                DateTime endTime = DateTime.Now;
                reviewResponse.ResponseTime = endTime - startTime;

                // Process response
                ProcessReviewResponse(reviewResponse);

                if (reviewResponse.IsSuccess)
                {
                    _requestId = reviewResponse.RequestId;
                    ViewState["RequestId"] = _requestId;

                    // Automatically perform enhance after successful review
                    await PerformEnhanceAsync();
                }
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

                DateTime startTime = DateTime.Now;
                // Call API service
                var enhanceResponse = await _apiService.EnhanceNotesAsync(enhanceRequest);
                DateTime endTime = DateTime.Now;
                enhanceResponse.ResponseTime = endTime - startTime;

                // Log raw response for debugging
                _logger.LogUserAction("Enhance Response", enhanceResponse.RawResponse);
                _logger.LogApiCall(WebConfigurationManager.AppSettings["ClaimKitApiUrl"], JsonConvert.SerializeObject(enhanceRequest), enhanceResponse.RawResponse, true);

                // Process response
                ProcessEnhanceResponse(enhanceResponse);
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

                DateTime startTime = DateTime.Now;

                // Call API service
                var generateClaimResponse = await _apiService.GenerateClaimAsync(generateClaimRequest);

                DateTime endTime = DateTime.Now;
                generateClaimResponse.ResponseTime = endTime - startTime;

                // Log raw response
                _logger.LogUserAction("Claim Response",
                    $"Response: {generateClaimResponse.RawResponse}, Response Time: {FormatResponseTime(generateClaimResponse.ResponseTime)}");

                // Process response
                ProcessGenerateClaimResponse(generateClaimResponse);
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
                DisplayError($"No response received from the medical records system. Response Time: {FormatResponseTime(response.ResponseTime)}");
                return;
            }

            // Log the response
            _logger.LogUserAction("Review Response Received",
                $"Success: {response.IsSuccess}, Message: {response.Message}, Response Time: {FormatResponseTime(response.ResponseTime)}");

            DateTime processingStartTime = DateTime.Now;

            if (response.IsSuccess)
            {
                // Store request ID for future calls
                _requestId = response.RequestId;
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("Request ID not returned from the medical records system.");
                    return;
                }

                ScriptManager.RegisterStartupScript(this, GetType(), "SetRequestId",
                            $"if(typeof window.setCurrentRequestId === 'function') {{ window.setCurrentRequestId('{_requestId}'); }} else {{ currentRequestId = '{_requestId}'; console.log('Set request ID to: {_requestId}'); }}", true);

                // Also, set the hidden field to ensure we have a fallback:
                if (hdnRequestId != null)
                {
                    hdnRequestId.Value = _requestId;
                }

                ViewState["RequestId"] = hdnRequestId.Value = _requestId;

                // Display success message
                lblStatus.Text = $"<div class='success'>Status: {FormatStatusMessage(response.Message)}</div>";
                lblRequestId.Text = $"<div>Request ID: {_requestId}</div>";

                // Set the client-side currentRequestId variable to enable modal transitions
                ScriptManager.RegisterStartupScript(this, GetType(), "SetRequestId",
                    $"currentRequestId = '{_requestId}'; console.log('Set request ID to: {_requestId}');", true);

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
                //ScriptManager.RegisterStartupScript(this, GetType(), "ShowReviewResults", "window.showReviewResultsModal();", true);

                //// Hide loading indicator at the start of processing the response
                //ScriptManager.RegisterStartupScript(this, GetType(), "HideLoadingFirst",
                //    "if(document.getElementById('loadingIndicator')) document.getElementById('loadingIndicator').style.display = 'none';", true);
            }
            else
            {
                // Display error message with a more user-friendly format
                DisplayError($"The clinical notes review could not be completed: {FormatErrorMessage(response.Message)}");
            }

            DateTime processingEndTime = DateTime.Now;
            TimeSpan processingTime = processingEndTime - processingStartTime;
            // Log the response
            _logger.LogUserAction("Review Response Processed",
                $"Message: {response.Message}, Processing Time: {FormatResponseTime(processingTime)}");
        }

        private void ProcessEnhanceResponse(EnhanceResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the system.");
                return;
            }

            // Log the response
            _logger.LogUserAction("Enhance Response Received",
                $"Success: {response.IsSuccess}, Message: {response.Message}, Response Time: {FormatResponseTime(response.ResponseTime)}");

            DateTime processingStartTime = DateTime.Now;

            if (response.IsSuccess)
            {
                if (response.Data != null && response.Data.EnhancedNotes != null)
                {
                    try
                    {
                        // Parse the enhanced notes data
                        JObject enhancedNotesObj;

                        // Handle different possible formats
                        if (response.Data.EnhancedNotes is JObject)
                        {
                            enhancedNotesObj = (JObject)response.Data.EnhancedNotes;
                        }
                        else if (response.Data.EnhancedNotes is string)
                        {
                            // If it's a JSON string, parse it
                            enhancedNotesObj = JObject.Parse((string)response.Data.EnhancedNotes);
                        }
                        else
                        {
                            // Serialize and then deserialize to JObject
                            string jsonStr = JsonConvert.SerializeObject(response.Data.EnhancedNotes);
                            enhancedNotesObj = JObject.Parse(jsonStr);
                        }

                        // Store the enhanced notes data for later use
                        StoreEnhancedNotesData(enhancedNotesObj);

                        // Format the enhanced notes using our formatter
                        //string formattedHtml = EnhancedNotesFormatter.FormatEnhancedNotes(enhancedNotesObj);

                        // Set the formatted HTML to the literal control
                        //litEnhancedNotes.Text = formattedHtml;
                        string serializedData = enhancedNotesObj.ToString(Formatting.None);
                        // Format for display - we'll use a hidden element to pass the raw JSON to JavaScript
                        litEnhancedNotes.Text = $"<div id='enhancedNotesDataContainer' style='display:none;' " +
                            $"data-json='{HttpUtility.HtmlAttributeEncode(serializedData)}'></div>" +
                            $"<div id='enhancedNotesDisplayContainer'></div>";

                        pnlEnhancedNotes.Visible = true;

                        // Set the modal to show after UpdatePanel refresh
                        hdnShowModal.Value = "showEnhancedNotesModal";

                        // Log the successful formatting
                        _logger.LogUserAction("Enhanced Notes Displayed",
                            $"Request ID: {_requestId}, Sections: {enhancedNotesObj["sections"]?.Count() ?? 0}, Response Time: {FormatResponseTime(response.ResponseTime)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error formatting enhanced notes", ex, JsonConvert.SerializeObject(response.Data.EnhancedNotes));

                        // Fall back to basic JSON display
                        litEnhancedNotes.Text = "<div class='error'>Could not format enhanced notes. Displaying raw data:</div>";
                        litEnhancedNotes.Text += JsonConvert.SerializeObject(response.Data.EnhancedNotes, Formatting.Indented);
                        pnlEnhancedNotes.Visible = true;

                        // Set the modal to show after UpdatePanel refresh
                        hdnShowModal.Value = "showEnhancedNotesModal";
                    }
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

            DateTime processingEndTime = DateTime.Now;
            TimeSpan processingTime = processingEndTime - processingStartTime;
            // Log the response
            _logger.LogUserAction("Enhance Response Processed",
                $"Message: {response.Message}, Processing Time: {FormatResponseTime(processingTime)}");
        }

        private void ProcessGenerateClaimResponse(GenerateClaimResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the system.");
                return;
            }
            // Log the response
            _logger.LogUserAction("Claim Response Received",
                $"Success: {response.IsSuccess}, Message: {response.Message}, Response Time: {FormatResponseTime(response.ResponseTime)}");

            DateTime processingStartTime = DateTime.Now;

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
                var errorMessage = response.Message.ToString();
                var errorreason = JsonConvert.DeserializeObject<dynamic>(response.RawResponse)?.message?.ToString();
                // Display error message
                DisplayError($"The insurance claim generation could not be completed: {FormatErrorMessage(errorreason)} and {FormatErrorMessage(response.Message)}, Response Time: {FormatResponseTime(response.ResponseTime)}");
            }

            DateTime processingEndTime = DateTime.Now;
            TimeSpan processingTime = processingEndTime - processingStartTime;
            // Log the response
            _logger.LogUserAction("Claim Response Processed",
                $"Message: {response.Message}, Processing Time: {FormatResponseTime(processingTime)}");
        }

        private void StoreEnhancedNotesData(JObject enhancedNotesObj)
        {
            string jsonStr = enhancedNotesObj.ToString(Formatting.None);
            // Store in ViewState for postbacks
            ViewState["EnhancedNotesData"] = jsonStr;
            // Store in session for later use
            Session["EnhancedNotesData"] = jsonStr;
            // Also store in hidden field for JavaScript access
            if (hdnEnhancedNotesData != null)
            {
                hdnEnhancedNotesData.Value = jsonStr;
            }
        }

        //private void ProcessEnhanceResponse(EnhanceResponse response)
        //{
        //    if (response == null)
        //    {
        //        DisplayError("No response received from the system.");
        //        return;
        //    }

        //    if (response.IsSuccess)
        //    {
        //        if (response.Data != null && response.Data.EnhancedNotes != null)
        //        {
        //            // Display enhanced notes
        //            litEnhancedNotes.Text = JsonConvert.SerializeObject(response.Data.EnhancedNotes, Formatting.Indented);
        //            pnlEnhancedNotes.Visible = true;

        //            // Set the modal to show after UpdatePanel refresh
        //            hdnShowModal.Value = "showEnhancedNotesModal";
        //        }
        //        else
        //        {
        //            DisplayError("No enhanced clinical notes were found in the response.");
        //        }
        //    }
        //    else
        //    {
        //        // Display error message
        //        DisplayError($"The clinical notes enhancement could not be completed: {FormatErrorMessage(response.Message)}");
        //    }
        //}

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

                        // Parse and display JSON feedback if available
                        if (!string.IsNullOrEmpty(category.Feedback))
                        {
                            try
                            {
                                var feedbackObj = JObject.Parse(category.Feedback);
                                content.AppendLine($"  <div class=\"feedback-container\">");

                                // Process each step in the feedback
                                foreach (var step in feedbackObj.Properties())
                                {
                                    content.AppendLine($"    <div class=\"feedback-step\">");
                                    content.AppendLine($"      <h4>{step.Name}</h4>");

                                    // Process categories within each step
                                    if (step.Value is JObject stepObject)
                                    {
                                        foreach (var feedbackCategory in stepObject.Properties())
                                        {
                                            content.AppendLine($"      <div class=\"feedback-category\">");
                                            content.AppendLine($"        <h5>{feedbackCategory.Name}</h5>");

                                            // Process details within each category
                                            if (feedbackCategory.Value is JObject categoryObject)
                                            {
                                                // Display result
                                                if (categoryObject["result"] != null)
                                                {
                                                    string resultClass = DetermineResultClass(categoryObject["result"].ToString());
                                                    content.AppendLine($"        <div class=\"feedback-result {resultClass}\">");
                                                    content.AppendLine($"          <strong>Result:</strong> {categoryObject["result"]}</div>");
                                                }

                                                // Display reasoning
                                                if (categoryObject["reasoning"] != null)
                                                {
                                                    content.AppendLine($"        <div class=\"feedback-reasoning\">");
                                                    content.AppendLine($"          <strong>Reasoning:</strong> {categoryObject["reasoning"]}</div>");
                                                }
                                            }

                                            content.AppendLine($"      </div>"); // Close feedback-category
                                        }
                                    }

                                    content.AppendLine($"    </div>"); // Close feedback-step
                                }

                                content.AppendLine($"  </div>"); // Close feedback-container
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error parsing feedback JSON", ex, category.Feedback);
                                content.AppendLine($"  <div class=\"section-error\">Could not parse detailed feedback information.</div>");
                            }
                        }

                        content.AppendLine($"</div>"); // Close category-content
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

        // Helper method to determine CSS class based on result text
        private string DetermineResultClass(string result)
        {
            string lowerResult = result.ToLower();

            if (lowerResult.Contains("consistent") ||
                lowerResult.Contains("necessary") ||
                lowerResult.Contains("relevant") ||
                lowerResult.Contains("complete"))
            {
                return "result-positive";
            }
            else if (lowerResult.Contains("inconsistent") ||
                     lowerResult.Contains("unnecessary") ||
                     lowerResult.Contains("irrelevant") ||
                     lowerResult.Contains("incomplete"))
            {
                return "result-negative";
            }

            return "result-neutral";
        }

        private string FormatResponseTime(TimeSpan timeSpan)
        {
            int minutes = (int)timeSpan.TotalMinutes;
            int seconds = (int)(timeSpan.TotalSeconds - (minutes * 60));
            int milliseconds = timeSpan.Milliseconds;

            if (minutes > 0)
            {
                return $"{minutes} m {seconds}.{milliseconds:D3} s";
            }
            else
            {
                return $"{seconds}.{milliseconds:D3} s";
            }
        }

        private void ResetResultPanels()
        {
            pnlReviewResults.Visible = false;
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;
            pnlFinalNotes.Visible = false;
            pnlError.Visible = false;
            pnlConfirmation.Visible = false;
        }

        private void ResetWorkflowPanels()
        {
            // Hide all result panels except the confirmation
            pnlReviewResults.Visible = false;
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;
            pnlFinalNotes.Visible = false;
            pnlError.Visible = false;

            // Hide action buttons
            pnlActionButtons.Visible = false;

            // Clear request ID to start fresh workflow if needed
            _requestId = null;
            ViewState["RequestId"] = null;

            // Don't clear txtDoctorNotes as we've just updated it with the final notes
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

        private void ShowEnhancedNotesModal()
        {
            // Make sure the enhanced notes panel is visible
            pnlEnhancedNotes.Visible = true;

            // Set the modal to show after UpdatePanel refresh
            hdnShowModal.Value = "showEnhancedNotesModal";

            // Hide the final notes panel if it's visible
            pnlFinalNotes.Visible = false;
        }

        private void PrepareAndShowFinalNotes(List<string> selectedSectionIds)
        {
            try
            {
                // Get the enhanced notes data
                JObject enhancedNotes = null;

                // Try hidden field first
                if (!string.IsNullOrEmpty(hdnEnhancedNotesData.Value))
                {
                    try
                    {
                        enhancedNotes = JObject.Parse(hdnEnhancedNotesData.Value);
                        _logger.LogUserAction("Retrieved enhanced notes from hidden field",
                            $"Size: {hdnEnhancedNotesData.Value.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error parsing enhanced notes from hidden field", ex);
                    }
                }

                // If that failed, try ViewState
                if (enhancedNotes == null && ViewState["EnhancedNotesData"] != null)
                {
                    try
                    {
                        string jsonData = ViewState["EnhancedNotesData"] as string;
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            enhancedNotes = JObject.Parse(jsonData);
                            _logger.LogUserAction("Retrieved enhanced notes from ViewState",
                                $"Size: {jsonData.Length} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error parsing enhanced notes from ViewState", ex);
                    }
                }

                // As a last resort, try Session
                if (enhancedNotes == null && Session["EnhancedNotesData"] != null)
                {
                    try
                    {
                        string jsonData = Session["EnhancedNotesData"] as string;
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            enhancedNotes = JObject.Parse(jsonData);
                            _logger.LogUserAction("Retrieved enhanced notes from Session",
                                $"Size: {jsonData.Length} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error parsing enhanced notes from Session", ex);
                    }
                }

                // If we couldn't get enhanced notes data, show error
                if (enhancedNotes == null)
                {
                    _logger.LogError("Enhanced notes data not found", null,
                        $"Looked in hidden field, ViewState, and Session. Selected sections: {string.Join(", ", selectedSectionIds)}");

                    DisplayError("Could not retrieve enhanced notes data. Please try enhancing your notes again.");
                    return;
                }

                // Build the final notes
                StringBuilder finalNotes = new StringBuilder();

                // Add header information
                finalNotes.AppendLine("# Patient Medical Record");
                finalNotes.AppendLine();
                finalNotes.AppendLine($"Date of Documentation: {DateTime.Now:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(txtPatientId.Text))
                {
                    finalNotes.AppendLine($"Patient ID: {txtPatientId.Text}");
                }

                if (!string.IsNullOrEmpty(txtInsuranceCompany.Text))
                {
                    finalNotes.AppendLine($"Insurance: {txtInsuranceCompany.Text}" +
                        (!string.IsNullOrEmpty(txtPolicyId.Text) ? $" (Policy: {txtPolicyId.Text})" : ""));
                }

                finalNotes.AppendLine();
                finalNotes.AppendLine($"Clinician: {txtDoctorName.Text}");

                if (!string.IsNullOrEmpty(txtDoctorSpecialization.Text))
                {
                    finalNotes.AppendLine($"Specialization: {txtDoctorSpecialization.Text}");
                }

                if (!string.IsNullOrEmpty(txtDoctorId.Text))
                {
                    finalNotes.AppendLine($"Clinician ID: {txtDoctorId.Text}");
                }

                finalNotes.AppendLine();

                // Check if enhanced notes has sections
                bool sectionsProcessed = false;

                if (enhancedNotes["sections"] != null && enhancedNotes["sections"].Type == JTokenType.Object)
                {
                    JObject sections = (JObject)enhancedNotes["sections"];

                    // Process each selected section
                    foreach (string sectionId in selectedSectionIds)
                    {
                        if (sections[sectionId] != null)
                        {
                            // Log section type for debugging
                            _logger.LogUserAction("Processing section",
                                $"Section ID: {sectionId}, Type: {sections[sectionId].Type}");
                            // Format the section and add to final notes
                            finalNotes.Append(FormatSectionForFinalNotes(sectionId, sections[sectionId]));
                            sectionsProcessed = true;
                        }
                        else
                        {
                            _logger.LogUserAction("Section not found",
                                $"Section ID: {sectionId} was not found in the enhanced notes");
                        }
                    }
                }

                // If no sections were processed, add a fallback
                if (!sectionsProcessed)
                {
                    finalNotes.AppendLine("## Clinical Notes");
                    finalNotes.AppendLine();
                    finalNotes.AppendLine("No valid sections were found in the enhanced notes. Please contact support if this issue persists.");
                    finalNotes.AppendLine();
                }

                // Add footer
                finalNotes.AppendLine("---");
                finalNotes.AppendLine("Generated by ClaimKit Medical Documentation Assistant");

                if (!string.IsNullOrEmpty(_requestId))
                {
                    finalNotes.AppendLine($"Request ID: {_requestId}");
                }

                // Set the final notes text
                txtFinalNotes.Text = finalNotes.ToString();

                // Show the panel for final editing
                pnlFinalNotes.Visible = true;

                // Log success
                _logger.LogUserAction("Final Notes Prepared",
                    $"Request ID: {_requestId}, Selected Sections: {string.Join(", ", selectedSectionIds)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error preparing final notes", ex);
                DisplayError("There was an issue preparing the final clinical notes. Please try again.");
            }
        }

        private string FormatSectionForFinalNotes(string sectionId, JToken sectionData)
        {
            var result = new StringBuilder();

            try
            {
                // Get section title from section ID
                string title = FormatSectionTitle(sectionId);

                // If it's an object with a title property, use that
                if (sectionData.Type == JTokenType.Object && sectionData["title"] != null)
                {
                    title = sectionData["title"].ToString();
                }

                // Add section header
                result.AppendLine($"## {title}");
                result.AppendLine();

                // Process section content based on its type
                if (sectionData.Type == JTokenType.Object)
                {
                    // Handle object section (with properties)
                    var sectionObj = (JObject)sectionData;

                    // Format fields
                    if (sectionObj["fields"] != null && sectionObj["fields"].Type == JTokenType.Object)
                    {
                        var fields = (JObject)sectionObj["fields"];
                        foreach (var field in fields.Properties())
                        {
                            string fieldName = EnhancedNotesFormatter.FormatFieldName(field.Name);
                            result.AppendLine($"**{fieldName}:** {field.Value}");
                        }
                        result.AppendLine();
                    }

                    // Format subsections
                    if (sectionObj["subsections"] != null && sectionObj["subsections"].Type == JTokenType.Object)
                    {
                        var subsections = (JObject)sectionObj["subsections"];
                        foreach (var subsection in subsections.Properties())
                        {
                            string subsectionTitle = subsection.Name;
                            if (subsection.Value["title"] != null)
                            {
                                subsectionTitle = subsection.Value["title"].ToString();
                            }

                            result.AppendLine($"### {subsectionTitle}");
                            result.AppendLine();

                            // Process fields in subsection
                            if (subsection.Value["fields"] != null)
                            {
                                var fields = (JObject)subsection.Value["fields"];
                                foreach (var field in fields.Properties())
                                {
                                    string fieldName = EnhancedNotesFormatter.FormatFieldName(field.Name);
                                    result.AppendLine($"**{fieldName}:** {field.Value}");
                                }
                                result.AppendLine();
                            }

                            // Process items list
                            if (subsection.Value["items"] != null && subsection.Value["items"].Type == JTokenType.Array)
                            {
                                foreach (var item in subsection.Value["items"])
                                {
                                    if (item.Type == JTokenType.String)
                                    {
                                        result.AppendLine($"- {item}");
                                    }
                                    else if (item.Type == JTokenType.Object)
                                    {
                                        // Handle complex items
                                        var itemObj = (JObject)item;
                                        if (itemObj["name"] != null)
                                        {
                                            string itemText = itemObj["name"].ToString();

                                            // Add codes if available
                                            if (itemObj["icd_10_cm_code"] != null)
                                            {
                                                itemText += $" (ICD-10: {itemObj["icd_10_cm_code"]})";
                                            }
                                            else if (itemObj["cpt_code"] != null)
                                            {
                                                itemText += $" (CPT: {itemObj["cpt_code"]})";
                                            }

                                            // Add description if available
                                            if (itemObj["description"] != null)
                                            {
                                                itemText += $" - {itemObj["description"]}";
                                            }

                                            result.AppendLine($"- {itemText}");
                                        }
                                    }
                                }
                                result.AppendLine();
                            }
                        }
                    }
                }
                else if (sectionData.Type == JTokenType.Array)
                {
                    // Handle array section
                    var sectionArray = (JArray)sectionData;

                    // Add a heading for this array section
                    result.AppendLine($"### {title} Items");
                    result.AppendLine();

                    // Process each item in the array
                    foreach (var item in sectionArray)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            // Simple string item
                            result.AppendLine($"- {item}");
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            // Object item - try to extract key information
                            var itemObj = (JObject)item;

                            // Check for common properties
                            string itemText = "";

                            // Try to get name property
                            if (itemObj["name"] != null)
                            {
                                itemText = $"**{itemObj["name"]}**";
                            }

                            // Try to get dosage, frequency, duration (for medications)
                            if (itemObj["dosage"] != null)
                            {
                                itemText += $" - {itemObj["dosage"]}";
                            }

                            if (itemObj["frequency"] != null)
                            {
                                itemText += $" {itemObj["frequency"]}";
                            }

                            if (itemObj["duration"] != null)
                            {
                                itemText += $" ({itemObj["duration"]})";
                            }

                            // Try to get description
                            if (itemObj["description"] != null)
                            {
                                itemText += $" - {itemObj["description"]}";
                            }

                            // If no recognized properties, just use the whole object
                            if (string.IsNullOrEmpty(itemText))
                            {
                                itemText = item.ToString();
                            }

                            result.AppendLine($"- {itemText}");
                        }
                        else
                        {
                            // Any other type of value
                            result.AppendLine($"- {item}");
                        }
                    }
                    result.AppendLine();
                }
                else
                {
                    // For any other data type, just convert to string
                    result.AppendLine(sectionData.ToString());
                    result.AppendLine();
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing
                _logger.LogError($"Error formatting section '{sectionId}'", ex);
                result.AppendLine($"**Error processing section: {ex.Message}**");
                result.AppendLine();
            }

            return result.ToString();
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

        //private void PrepareAndShowFinalNotes(List<string> selectedNotes)
        //{
        //    try
        //    {
        //        // Combine selected notes into a single document
        //        StringBuilder finalNotes = new StringBuilder();
        //        foreach (var note in selectedNotes)
        //        {
        //            // Try to parse note identifier
        //            string[] parts = note.Split('-');
        //            if (parts.Length >= 2)
        //            {
        //                // Handle section-specific notes if applicable
        //                if (parts.Length > 2)
        //                {
        //                    string section = parts[1];
        //                    finalNotes.AppendLine($"=== {FormatSectionTitle(section)} ===");
        //                }

        //                // Look up the actual note content - this is simplified here
        //                // In a real implementation, we would parse the original JSON to extract actual notes
        //                finalNotes.AppendLine($"Enhanced clinical note approved by Dr. {txtDoctorName.Text}");
        //                finalNotes.AppendLine();
        //            }
        //        }

        //        // Set the final notes text
        //        txtFinalNotes.Text = finalNotes.ToString();

        //        // Show the panel for final editing
        //        pnlFinalNotes.Visible = true;

        //        // Log the action
        //        _logger.LogUserAction("Final Notes Prepared", $"Request ID: {_requestId}, Notes Count: {selectedNotes.Count}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error preparing final notes", ex);
        //        DisplayError("There was an issue preparing the final clinical notes. Please try again.");
        //    }
        //}

        //private void PrepareAndShowFinalNotes(List<string> selectedSectionIds)
        //{
        //    try
        //    {
        //        // Get the enhanced notes object from the viewstate or session
        //        JObject enhancedNotes = null;

        //        // Try to parse the last API response
        //        if (!string.IsNullOrEmpty(ViewState["EnhancedNotesData"] as string))
        //        {
        //            enhancedNotes = JObject.Parse((string)ViewState["EnhancedNotesData"]);
        //        }
        //        else if (Session["EnhancedNotesData"] != null)
        //        {
        //            enhancedNotes = Session["EnhancedNotesData"] as JObject;
        //        }

        //        StringBuilder finalNotes = new StringBuilder();

        //        if (enhancedNotes != null && enhancedNotes["sections"] != null)
        //        {
        //            // Add title if available
        //            if (enhancedNotes["title"] != null)
        //            {
        //                finalNotes.AppendLine($"# {enhancedNotes["title"]}");
        //                finalNotes.AppendLine();
        //            }
        //            else
        //            {
        //                finalNotes.AppendLine("# Patient Medical Record");
        //                finalNotes.AppendLine();
        //            }

        //            // Add record date
        //            finalNotes.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd}");
        //            finalNotes.AppendLine();

        //            // Add clinician information
        //            finalNotes.AppendLine($"**Clinician:** {txtDoctorName.Text}");
        //            finalNotes.AppendLine($"**Specialization:** {txtDoctorSpecialization.Text}");
        //            finalNotes.AppendLine($"**Clinician ID:** {txtDoctorId.Text}");
        //            finalNotes.AppendLine();

        //            // Process selected sections
        //            JObject sections = (JObject)enhancedNotes["sections"];
        //            foreach (string sectionId in selectedSectionIds)
        //            {
        //                if (sections[sectionId] != null)
        //                {
        //                    // Format and add section content
        //                    finalNotes.Append(FormatSectionForFinalNotes(sectionId, sections[sectionId]));
        //                }
        //            }
        //        }
        //        else
        //        {
        //            // Fallback if we can't access the enhanced notes data
        //            finalNotes.AppendLine("# Patient Medical Record");
        //            finalNotes.AppendLine();
        //            finalNotes.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd}");
        //            finalNotes.AppendLine();
        //            finalNotes.AppendLine($"**Clinician:** {txtDoctorName.Text}");
        //            finalNotes.AppendLine($"**Specialization:** {txtDoctorSpecialization.Text}");
        //            finalNotes.AppendLine();

        //            // Basic section for each selected note
        //            foreach (string sectionId in selectedSectionIds)
        //            {
        //                finalNotes.AppendLine($"## {FormatSectionTitle(sectionId)}");
        //                finalNotes.AppendLine();
        //                finalNotes.AppendLine($"Enhanced section approved by Dr. {txtDoctorName.Text}");
        //                finalNotes.AppendLine();
        //            }
        //        }

        //        // Set the final notes text
        //        txtFinalNotes.Text = finalNotes.ToString();

        //        // Show the panel for final editing
        //        pnlFinalNotes.Visible = true;

        //        // Log the action
        //        _logger.LogUserAction("Final Notes Prepared",
        //            $"Request ID: {_requestId}, Sections Count: {selectedSectionIds.Count}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error preparing final notes", ex);
        //        DisplayError("There was an issue preparing the final clinical notes. Please try again.");
        //    }
        //}

        //private void PrepareAndShowFinalNotes(List<string> selectedSectionIds)
        //{
        //    try
        //    {
        //        // Check if we have saved notes from a previous edit
        //        if (Session["SavedFinalNotes"] != null)
        //        {
        //            txtFinalNotes.Text = Session["SavedFinalNotes"].ToString();
        //            Session.Remove("SavedFinalNotes"); // Clear after using

        //            _logger.LogUserAction("Restored Saved Final Notes", $"Request ID: {_requestId}");

        //            // Show the final notes panel
        //            pnlFinalNotes.Visible = true;
        //            return;
        //        }

        //        // Get the enhanced notes object from the ViewState
        //        JObject enhancedNotes = null;

        //        // Try to parse the last API response
        //        if (!string.IsNullOrEmpty(hdnEnhancedNotesData.Value))
        //        {
        //            enhancedNotes = JObject.Parse(hdnEnhancedNotesData.Value);
        //        }
        //        else if (ViewState["EnhancedNotesData"] != null)
        //        {
        //            enhancedNotes = JObject.Parse((string)ViewState["EnhancedNotesData"]);
        //        }
        //        else if (Session["EnhancedNotesData"] != null)
        //        {
        //            enhancedNotes = Session["EnhancedNotesData"] as JObject;
        //        }

        //        StringBuilder finalNotes = new StringBuilder();

        //        if (enhancedNotes != null && enhancedNotes["sections"] != null)
        //        {
        //            // Add title if available
        //            if (enhancedNotes["title"] != null)
        //            {
        //                finalNotes.AppendLine($"# {enhancedNotes["title"]}");
        //                finalNotes.AppendLine();
        //            }
        //            else
        //            {
        //                finalNotes.AppendLine("# Patient Medical Record");
        //                finalNotes.AppendLine();
        //            }

        //            // Add record metadata
        //            finalNotes.AppendLine($"Date of Documentation: {DateTime.Now:yyyy-MM-dd}");
        //            finalNotes.AppendLine($"Patient ID: {txtPatientId.Text}");
        //            if (!string.IsNullOrEmpty(txtInsuranceCompany.Text))
        //            {
        //                finalNotes.AppendLine($"Insurance: {txtInsuranceCompany.Text} (Policy: {txtPolicyId.Text})");
        //            }
        //            finalNotes.AppendLine();

        //            // Add clinician information
        //            finalNotes.AppendLine($"Clinician: {txtDoctorName.Text}");
        //            if (!string.IsNullOrEmpty(txtDoctorSpecialization.Text))
        //            {
        //                finalNotes.AppendLine($"Specialization: {txtDoctorSpecialization.Text}");
        //            }
        //            if (!string.IsNullOrEmpty(txtDoctorId.Text))
        //            {
        //                finalNotes.AppendLine($"Clinician ID: {txtDoctorId.Text}");
        //            }
        //            finalNotes.AppendLine();

        //            // Process selected sections
        //            JObject sections = (JObject)enhancedNotes["sections"];
        //            foreach (string sectionId in selectedSectionIds)
        //            {
        //                if (sections[sectionId] != null)
        //                {
        //                    // Format and add section content
        //                    finalNotes.Append(FormatSectionForFinalNotes(sectionId, sections[sectionId]));
        //                }
        //            }

        //            // Add footer
        //            finalNotes.AppendLine();
        //            finalNotes.AppendLine("---");
        //            finalNotes.AppendLine($"Generated by ClaimKit Medical Documentation Assistant");
        //            finalNotes.AppendLine($"Request ID: {_requestId}");
        //        }
        //        else
        //        {
        //            // Fallback if we can't access the enhanced notes data
        //            finalNotes.AppendLine("# Patient Medical Record");
        //            finalNotes.AppendLine();
        //            finalNotes.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}");
        //            finalNotes.AppendLine($"Patient ID: {txtPatientId.Text}");
        //            finalNotes.AppendLine();
        //            finalNotes.AppendLine($"Clinician: {txtDoctorName.Text}");
        //            if (!string.IsNullOrEmpty(txtDoctorSpecialization.Text))
        //            {
        //                finalNotes.AppendLine($"Specialization: {txtDoctorSpecialization.Text}");
        //            }
        //            finalNotes.AppendLine();

        //            // Basic section for each selected note
        //            foreach (string sectionId in selectedSectionIds)
        //            {
        //                finalNotes.AppendLine($"## {FormatSectionTitle(sectionId)}");
        //                finalNotes.AppendLine();
        //                finalNotes.AppendLine($"Enhanced documentation section approved by Dr. {txtDoctorName.Text}");
        //                finalNotes.AppendLine();
        //            }
        //        }

        //        // Set the final notes text
        //        txtFinalNotes.Text = finalNotes.ToString();

        //        // Show the panel for final editing
        //        pnlFinalNotes.Visible = true;

        //        // Log the action
        //        _logger.LogUserAction("Final Notes Prepared",
        //            $"Request ID: {_requestId}, Sections Count: {selectedSectionIds.Count}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error preparing final notes", ex);
        //        DisplayError("There was an issue preparing the final clinical notes. Please try again.");
        //    }
        //}

        //private string FormatSectionForFinalNotes(string sectionId, JToken sectionData)
        //{
        //    var result = new StringBuilder();

        //    // Get section title
        //    string title = FormatSectionTitle(sectionId);
        //    if (sectionData["title"] != null)
        //    {
        //        title = sectionData["title"].ToString();
        //    }

        //    // Add section header
        //    result.AppendLine($"## {title}");
        //    result.AppendLine();

        //    if (sectionData.Type == JTokenType.Object)
        //    {
        //        var sectionObj = (JObject)sectionData;

        //        // Format fields
        //        if (sectionObj["fields"] != null && sectionObj["fields"].Type == JTokenType.Object)
        //        {
        //            var fields = (JObject)sectionObj["fields"];
        //            foreach (var field in fields.Properties())
        //            {
        //                string fieldName = EnhancedNotesFormatter.FormatFieldName(field.Name);
        //                result.AppendLine($"**{fieldName}:** {field.Value}");
        //            }
        //            result.AppendLine();
        //        }

        //        // Format subsections
        //        if (sectionObj["subsections"] != null && sectionObj["subsections"].Type == JTokenType.Object)
        //        {
        //            var subsections = (JObject)sectionObj["subsections"];
        //            foreach (var subsection in subsections.Properties())
        //            {
        //                string subsectionTitle = subsection.Name;
        //                if (subsection.Value["title"] != null)
        //                {
        //                    subsectionTitle = subsection.Value["title"].ToString();
        //                }

        //                result.AppendLine($"### {subsectionTitle}");
        //                result.AppendLine();

        //                // Process fields in subsection
        //                if (subsection.Value["fields"] != null)
        //                {
        //                    var fields = (JObject)subsection.Value["fields"];
        //                    foreach (var field in fields.Properties())
        //                    {
        //                        string fieldName = EnhancedNotesFormatter.FormatFieldName(field.Name);
        //                        result.AppendLine($"**{fieldName}:** {field.Value}");
        //                    }
        //                    result.AppendLine();
        //                }

        //                // Process items list
        //                if (subsection.Value["items"] != null && subsection.Value["items"].Type == JTokenType.Array)
        //                {
        //                    foreach (var item in subsection.Value["items"])
        //                    {
        //                        if (item.Type == JTokenType.String)
        //                        {
        //                            result.AppendLine($"- {item}");
        //                        }
        //                        else if (item.Type == JTokenType.Object)
        //                        {
        //                            // Handle complex items
        //                            var itemObj = (JObject)item;
        //                            if (itemObj["name"] != null)
        //                            {
        //                                string itemText = itemObj["name"].ToString();

        //                                // Add codes if available
        //                                if (itemObj["icd_10_cm_code"] != null)
        //                                {
        //                                    itemText += $" (ICD-10: {itemObj["icd_10_cm_code"]})";
        //                                }
        //                                else if (itemObj["cpt_code"] != null)
        //                                {
        //                                    itemText += $" (CPT: {itemObj["cpt_code"]})";
        //                                }

        //                                // Add description if available
        //                                if (itemObj["description"] != null)
        //                                {
        //                                    itemText += $" - {itemObj["description"]}";
        //                                }

        //                                result.AppendLine($"- {itemText}");
        //                            }
        //                        }
        //                    }
        //                    result.AppendLine();
        //                }
        //            }
        //        }

        //        // Format conditions
        //        if (sectionObj["conditions"] != null && sectionObj["conditions"].Type == JTokenType.Array)
        //        {
        //            foreach (var condition in sectionObj["conditions"])
        //            {
        //                if (condition.Type == JTokenType.Object)
        //                {
        //                    var condObj = (JObject)condition;
        //                    string condTitle = condObj["title"]?.ToString() ?? "Condition";
        //                    string condDesc = condObj["description"]?.ToString() ?? "";

        //                    result.AppendLine($"### {condTitle}");
        //                    result.AppendLine(condDesc);
        //                    result.AppendLine();
        //                }
        //            }
        //        }

        //        // Format procedures
        //        if (sectionObj["procedures"] != null && sectionObj["procedures"].Type == JTokenType.Array)
        //        {
        //            result.AppendLine("### Procedures");
        //            result.AppendLine();
        //            foreach (var procedure in sectionObj["procedures"])
        //            {
        //                if (procedure.Type == JTokenType.Object)
        //                {
        //                    var procObj = (JObject)procedure;
        //                    string procName = procObj["name"]?.ToString() ?? "Procedure";
        //                    string cptCode = procObj["cpt_code"]?.ToString() ?? "";

        //                    string procText = procName;
        //                    if (!string.IsNullOrEmpty(cptCode))
        //                    {
        //                        procText += $" (CPT: {cptCode})";
        //                    }

        //                    result.AppendLine($"- {procText}");
        //                }
        //            }
        //            result.AppendLine();
        //        }
        //    }
        //    else if (sectionData.Type == JTokenType.Array)
        //    {
        //        // Handle array sections like medications
        //        if (sectionId.ToLower() == "medications")
        //        {
        //            foreach (var med in sectionData)
        //            {
        //                if (med.Type == JTokenType.Object)
        //                {
        //                    var medObj = (JObject)med;
        //                    string name = medObj["name"]?.ToString() ?? "";
        //                    string dosage = medObj["dosage"]?.ToString() ?? "";
        //                    string frequency = medObj["frequency"]?.ToString() ?? "";
        //                    string duration = medObj["duration"]?.ToString() ?? "";
        //                    string description = medObj["description"]?.ToString() ?? "";
        //                    string code = medObj["medication_code"]?.ToString() ?? "";

        //                    result.AppendLine($"### {name} {(string.IsNullOrEmpty(code) ? "" : $"({code})")}");
        //                    result.AppendLine();
        //                    if (!string.IsNullOrEmpty(dosage))
        //                        result.AppendLine($"**Dosage:** {dosage}");
        //                    if (!string.IsNullOrEmpty(frequency))
        //                        result.AppendLine($"**Frequency:** {frequency}");
        //                    if (!string.IsNullOrEmpty(duration))
        //                        result.AppendLine($"**Duration:** {duration}");
        //                    if (!string.IsNullOrEmpty(description))
        //                        result.AppendLine($"**Description:** {description}");
        //                    result.AppendLine();
        //                }
        //            }
        //        }
        //        else
        //        {
        //            // Generic array handling
        //            foreach (var item in sectionData)
        //            {
        //                result.AppendLine($"- {item}");
        //            }
        //            result.AppendLine();
        //        }
        //    }

        //    return result.ToString();
        //}

        //// Helper methods for formatting
        //private string FormatFieldName(string fieldName)
        //{
        //    if (string.IsNullOrEmpty(fieldName))
        //        return string.Empty;

        //    // Replace underscores with spaces
        //    string result = fieldName.Replace('_', ' ');

        //    // Handle camelCase by adding spaces before capital letters
        //    result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

        //    // Title case the field name
        //    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
        //}

        #endregion

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
    }
}