using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using ClaimKitv1.Models.Responses;
using ClaimKitv1.Services;
using ClaimKitv1.Helpers;
using Newtonsoft.Json;
using System.Linq;
using ClaimKitv1.Models.EMR;
using ClaimKitv1.Models;
using System.Web.Configuration;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ClaimKitv1
{
    public partial class Default : System.Web.UI.Page
    {
        #region Private Fields
        private readonly IClaimKitApiService _apiService;
        private readonly IEmrApiService _emrApiService;
        private readonly LoggingService _logger;
        private readonly IValidationService _validationService;
        private readonly IUiManager _uiManager;
        private readonly IFormDataService _formDataService;
        private readonly INotesProcessingService _notesProcessingService;
        private readonly IWorkflowManager _workflowService;
        private readonly PerformanceTracker _performanceTracker;
        private readonly IEmrIntegrationService _emrIntegrationService;

        private string _requestId;
        #endregion

        #region Constructor and Initialization
        public Default() : base()
        {
            _apiService = new ClaimKitApiService();
            _emrApiService = new EmrApiService();
            _logger = LoggingService.Instance;
            _validationService = new ValidationService(_logger);
            _uiManager = new UiManager(this, _logger);
            _formDataService = new FormDataService(_logger);
            _notesProcessingService = new NotesProcessingService(_logger);
            _workflowService = new WorkflowManager(this, _logger);
            _performanceTracker = new PerformanceTracker(_logger);
            _emrIntegrationService = new EmrIntegrationService(_emrApiService, _logger);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("Page_Load"))
            {
                try
                {
                    if (!IsPostBack)
                    {
                        InitializePage();
                    }
                    else
                    {
                        HandlePostBack();
                    }

                    RestoreSessionState();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in Page_Load", ex);
                    _uiManager.ShowError("An error occurred while loading the page. Please refresh and try again.");
                }
            }
        }

        private void InitializePage()
        {
            var initTask = Task.Run(async () =>
            {
                await InitializeFormDefaultsAsync();
            });

            RegisterAsyncTask(new PageAsyncTask(() => initTask));
            _logger.LogUserAction("Application Access", "User accessed ClaimKit application");
        }

        private async Task InitializeFormDefaultsAsync()
        {
            try
            {
                var config = await LoadConfigurationAsync();
                PopulateFormFields(config);

                _logger.LogUserAction("Form Initialized", "Default values loaded from configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing form defaults", ex);
            }
        }

        private async Task<FormData> LoadConfigurationAsync()
        {
            return await Task.FromResult(new FormData
            {
                InsuranceCompany = WebConfigurationManager.AppSettings["DefaultInsuranceCompany"],
                PolicyBand = WebConfigurationManager.AppSettings["DefaultPolicyBand"],
                DoctorName = WebConfigurationManager.AppSettings["DefaultDoctorName"],
                DoctorSpecialization = WebConfigurationManager.AppSettings["DefaultDoctorSpecialization"],
                ClinicalNotes = WebConfigurationManager.AppSettings["DefaultDoctorNotes"]
            });
        }

        private void PopulateFormFields(FormData config)
        {
            txtInsuranceCompany.Text = config.InsuranceCompany ?? "";
            txtDoctorName.Text = config.DoctorName ?? "";
            txtDoctorSpecialization.Text = config.DoctorSpecialization ?? "";
            txtDoctorNotes.Text = config.ClinicalNotes ?? "";

            SetPolicyBand(config.PolicyBand);
            SetEmrDefaults();
        }

        private void SetPolicyBand(string defaultPolicyBand)
        {
            if (string.IsNullOrEmpty(defaultPolicyBand)) return;

            if (ddlPolicyBand?.Items.FindByValue(defaultPolicyBand) != null)
            {
                ddlPolicyBand.SelectedValue = defaultPolicyBand;
            }
            else if (txtPolicyBand != null)
            {
                txtPolicyBand.Text = defaultPolicyBand;
            }
        }

        private void SetEmrDefaults()
        {
            txtEncounterId.Text = WebConfigurationManager.AppSettings["DefaultEncounterId"] ?? "";
            txtRegistrationId.Text = WebConfigurationManager.AppSettings["DefaultRegistrationId"] ?? "";
            txtHospitalLocationId.Text = WebConfigurationManager.AppSettings["DefaultHospitalLocationId"] ?? "";
            txtTemplateId.Text = WebConfigurationManager.AppSettings["DefaultTemplateId"] ?? "";
            txtPatientHistory.Text = WebConfigurationManager.AppSettings["DefaultPatientHistory"] ?? "";
        }

        private void HandlePostBack()
        {
            var modalToShow = hdnShowModal?.Value;
            if (!string.IsNullOrEmpty(modalToShow))
            {
                _logger.LogUserAction("Modal Trigger", $"Showing modal: {modalToShow}");

                if (modalToShow == "showEnhancedNotesModal")
                {
                    EnsureEnhancedNotesDataReady();
                }

                _uiManager.ShowModalAfterPostback(modalToShow);
                hdnShowModal.Value = "";
            }
        }

        private void EnsureEnhancedNotesDataReady()
        {
            if (hdnEnhancedNotesData != null && string.IsNullOrEmpty(hdnEnhancedNotesData.Value))
            {
                var enhancedData = ViewState["EnhancedNotesData"] as string ?? Session["EnhancedNotesData"] as string;
                if (!string.IsNullOrEmpty(enhancedData))
                {
                    hdnEnhancedNotesData.Value = enhancedData;
                    _logger.LogUserAction("Enhanced Notes Data Restored", "Data restored from ViewState/Session");
                }
            }
        }

        private void RestoreSessionState()
        {
            _requestId = ViewState["RequestId"]?.ToString();

            if (ViewState["EnhancedNotesData"] != null && hdnEnhancedNotesData != null)
            {
                hdnEnhancedNotesData.Value = ViewState["EnhancedNotesData"].ToString();
            }
        }
        #endregion

        #region Helper Methods for Form Data
        private FormData GetFormData()
        {
            return new FormData
            {
                ClinicalNotes = txtDoctorNotes.Text,
                InsuranceCompany = txtInsuranceCompany.Text,
                PolicyBand = GetSelectedPolicyBand(),
                PolicyId = txtPolicyId.Text,
                DoctorName = txtDoctorName.Text,
                DoctorSpecialization = txtDoctorSpecialization.Text,
                PatientIdentifier = GetPatientIdentifier()
            };
        }

        private EmrFormData GetEmrFormData()
        {
            return new EmrFormData
            {
                EncounterId = txtEncounterId.Text,
                RegistrationId = txtRegistrationId.Text,
                HospitalLocationId = txtHospitalLocationId.Text,
                TemplateId = txtTemplateId.Text
            };
        }

        private EmrDataRequest CreateEmrRequest(EmrFormData emrData)
        {
            return new EmrDataRequest
            {
                EncounterId = int.Parse(emrData.EncounterId),
                RegistrationId = int.Parse(emrData.RegistrationId),
                HospitalLocationId = string.IsNullOrEmpty(emrData.HospitalLocationId) ? 1 : int.Parse(emrData.HospitalLocationId),
                TemplateId = string.IsNullOrEmpty(emrData.TemplateId) ? 3024 : int.Parse(emrData.TemplateId),
                FacilityId = 11
            };
        }

        private string GetSelectedPolicyBand()
        {
            return ddlPolicyBand?.SelectedValue != "" ? ddlPolicyBand.SelectedValue
                   : !string.IsNullOrEmpty(txtPolicyBand?.Text) ? txtPolicyBand.Text
                   : "Standard";
        }

        private string GetPatientIdentifier()
        {
            return !string.IsNullOrEmpty(txtPatientId?.Text) ? txtPatientId.Text
                   : !string.IsNullOrEmpty(txtEncounterId.Text) ? txtEncounterId.Text
                   : "Unknown";
        }

        private List<string> GetSelectedSections()
        {
            var selectedNotesJson = hdnSelectedNotes?.Value;

            if (string.IsNullOrEmpty(selectedNotesJson) || selectedNotesJson == "[]")
            {
                return GetAllAvailableSections();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(selectedNotesJson) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error parsing selected sections", ex, selectedNotesJson);
                return new List<string>();
            }
        }

        private List<string> GetAllAvailableSections()
        {
            var enhancedDataJson = hdnEnhancedNotesData?.Value;
            if (string.IsNullOrEmpty(enhancedDataJson)) return new List<string>();

            try
            {
                var enhancedData = JObject.Parse(enhancedDataJson);
                if (enhancedData["sections"]?.Type == JTokenType.Object)
                {
                    return ((JObject)enhancedData["sections"]).Properties().Select(p => p.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error parsing enhanced notes data", ex);
            }

            return new List<string>();
        }
        #endregion

        #region Event Handlers
        protected async void btnLoadEmrData_Click(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("LoadEmrData"))
            {
                try
                {
                    var emrData = GetEmrFormData();
                    var validationResult = _validationService.ValidateEmrParameters(emrData);

                    if (!validationResult.IsValid)
                    {
                        _uiManager.ShowError($"Please fix the following issues:\n• {validationResult.FormattedMessage}");
                        return;
                    }

                    _uiManager.ShowLoadingIndicator();
                    _logger.LogUserAction("Load EMR Data Initiated", $"EncounterId: {emrData.EncounterId}");

                    // Use the new EMR Integration Service
                    var emrRequest = CreateEmrRequest(emrData);
                    var integrationResult = await _emrIntegrationService.LoadAndProcessEmrDataAsync(emrRequest);

                    if (integrationResult.IsSuccess)
                    {
                        ProcessEmrIntegrationResult(integrationResult);

                        var successMessage = $"EMR data successfully loaded and integrated. " +
                                           $"Retrieved: {integrationResult.EmrData.Vitals?.Count ?? 0} vital signs, " +
                                           $"{integrationResult.EmrData.Problems?.Count ?? 0} problems.";

                        if (integrationResult.ValidationWarnings.Any())
                        {
                            successMessage += $"\n\nWarnings: {string.Join(", ", integrationResult.ValidationWarnings)}";
                        }

                        _uiManager.ShowConfirmation(successMessage);
                    }
                    else
                    {
                        _uiManager.ShowError($"EMR integration failed: {integrationResult.Message}");

                        if (integrationResult.Exception != null)
                        {
                            _logger.LogError("EMR Integration Exception Details", integrationResult.Exception);
                        }

                        // NEW: fall back to defaults so the form stays usable
                        try
                        {
                            ClearEmrData(); // wipe any half-filled EMR fields first
                            await InitializeFormDefaultsAsync(); // reload defaults from config
                            _logger.LogUserAction("EMR Fallback Defaults Applied",
                                "EMR failed; populated form with default configuration values.");
                        }
                        catch (Exception ex2)
                        {
                            _logger.LogError("Error applying EMR fallback defaults", ex2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in EMR data loading process", ex);
                    _uiManager.ShowError("There was an issue loading data from the EMR system. Please try again.");
                }
                finally
                {
                    _uiManager.HideLoadingIndicator();
                }
            }
        }

        protected async void btnReviewNotes_Click(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("ReviewNotes"))
            {
                try
                {
                    ResetResultPanels();

                    var formData = GetFormData();
                    var validationResult = _validationService.ValidateClaimKitRequiredFields(formData);

                    if (!validationResult.IsValid)
                    {
                        _uiManager.ShowError($"Please complete the following required fields:\n• {validationResult.FormattedMessage}");
                        return;
                    }

                    _uiManager.ShowLoadingIndicator();
                    _uiManager.ShowProgressMessage("Reviewing and enhancing your clinical notes...", true);

                    await PerformReviewAndEnhanceAsync(formData);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error initiating review/enhance", ex);
                    _uiManager.ShowError("There was an issue starting the review and enhancement process. Please try again.");
                }
                finally
                {
                    _uiManager.HideLoadingIndicator();
                }
            }
        }

        protected async void btnViewEnhanceNotes_Click(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("ViewEnhanceNotes"))
            {
                try
                {
                    ResetResultPanels();

                    var formData = GetFormData();
                    var validationResult = _validationService.ValidateClaimKitRequiredFields(formData);

                    if (!validationResult.IsValid)
                    {
                        _uiManager.ShowError($"Please complete the following required fields:\n• {validationResult.FormattedMessage}");
                        return;
                    }

                    _uiManager.ShowLoadingIndicator();
                    _uiManager.ShowProgressMessage("Enhancing your clinical notes...", true);

                    await PerformReviewAndEnhanceAsync(formData);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in ViewEnhanceNotes_Click", ex);
                    _uiManager.ShowError("An error occurred while processing. Please try again.");
                }
                finally
                {
                    _uiManager.HideLoadingIndicator();
                }
            }
        }

        protected void btnServerApproveNotes_Click(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("ApproveNotes"))
            {
                try
                {
                    var selectedSections = GetSelectedSections();
                    if (!selectedSections.Any())
                    {
                        _uiManager.ShowError("No sections were selected. Please select at least one section to approve.");
                        return;
                    }

                    ProcessApprovedNotes(selectedSections);
                    _workflowService.SetStep("FinalizeNotes");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error approving notes", ex);
                    _uiManager.ShowError("There was an issue approving the enhanced notes. Please try again.");
                }
            }
        }

        protected void btnSaveFinalNotes_Click(object sender, EventArgs e)
        {
            using (var operation = _performanceTracker.StartOperation("SaveFinalNotes"))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(txtFinalNotes.Text))
                    {
                        _uiManager.ShowError("Final clinical notes cannot be empty. Please enter your notes.");
                        return;
                    }

                    var savedFilePath = SaveNotesToFile();
                    UpdateOriginalNotes();
                    ResetWorkflowPanels();

                    _uiManager.ShowConfirmation($"Your clinical notes have been successfully saved as {Path.GetFileName(savedFilePath)} and the claim information has been sent to the insurance company.");

                    _logger.LogUserAction("Final Notes Saved", $"Request ID: {_requestId}, File: {Path.GetFileName(savedFilePath)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error saving final notes", ex);
                    _uiManager.ShowError("There was an issue saving your final notes. Please try again.");
                }
            }
        }

        protected void btnClearEmrData_Click(object sender, EventArgs e)
        {
            try
            {
                ClearEmrData();
                _logger.LogUserAction("EMR Data Cleared", "User manually cleared EMR data");
                _uiManager.ShowConfirmation("EMR data has been cleared. You can now enter data manually or load fresh EMR data.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing EMR data", ex);
                _uiManager.ShowError("There was an issue clearing EMR data. Please try again.");
            }
        }

        protected void btnBackToEnhanced_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtFinalNotes?.Text))
                {
                    Session["SavedFinalNotes"] = txtFinalNotes.Text;
                }

                ShowEnhancedNotesModal();
                _logger.LogUserAction("Returned to Enhanced Notes", $"Request ID: {_requestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error returning to enhanced notes", ex);
                _uiManager.ShowError("There was an issue returning to the enhanced notes view. Please try again.");
            }
        }

        protected void btnServerApproveDiagnoses_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedDiagnosesJson = hdnSelectedDiagnoses?.Value;
                if (string.IsNullOrEmpty(selectedDiagnosesJson))
                {
                    _uiManager.ShowError("No diagnoses were selected. Please select at least one diagnosis to approve.");
                    return;
                }

                var selectedDiagnoses = JsonConvert.DeserializeObject<List<string>>(selectedDiagnosesJson);
                _logger.LogUserAction("Approved Claim Diagnoses", $"Request ID: {_requestId}, Diagnoses Count: {selectedDiagnoses.Count}");
                _uiManager.ShowConfirmation("The selected diagnoses have been approved and will be used for the insurance claim.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error approving diagnoses", ex);
                _uiManager.ShowError("There was an issue approving the diagnoses. Please try again.");
            }
        }

        protected void btnCloseError_Click(object sender, EventArgs e)
        {
            if (pnlError != null) pnlError.Visible = false;
        }

        protected void btnCloseConfirmation_Click(object sender, EventArgs e)
        {
            if (pnlConfirmation != null) pnlConfirmation.Visible = false;
        }

        protected void rptReviewCategories_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var litCategoryContent = (Literal)e.Item.FindControl("litCategoryContent");
            if (litCategoryContent != null && e.Item.DataItem is ReviewCategory category)
            {
                litCategoryContent.Text = FormatReviewCategory(category);
            }
        }
        #endregion

        #region Core Processing Methods
        private async Task PerformReviewAndEnhanceAsync(FormData formData)
        {
            //var currentTimestamp = GetCurrentUnixTimestamp();
            //var checkoutTime = currentTimestamp + (3600); // 1 hour offset

            //ViewState["CheckoutTime"] = checkoutTime;

            //var patientHistory = await ParsePatientHistoryAsync();
            //if (patientHistory == null) return;

            //var reviewRequest = CreateReviewRequest(formData, currentTimestamp, patientHistory);
            //var reviewResponse = await _apiService.ReviewNotesAsync(reviewRequest);

            //ProcessReviewResponse(reviewResponse);

            //if (reviewResponse.IsSuccess && !string.IsNullOrEmpty(reviewResponse.RequestId))
            //{
            //    await PerformAutoEnhanceAsync(reviewResponse.RequestId);
            //}

            var useNewApi = WebConfigurationManager.AppSettings["UseNewApi"] == "true";

            if (useNewApi)
            {
                await PerformReviewWithNewApiAsync(formData);
            }
            else
            {
                var currentTimestamp = GetCurrentUnixTimestamp();
                var checkoutTime = currentTimestamp + (3600);

                ViewState["CheckoutTime"] = checkoutTime;

                var patientHistory = await ParsePatientHistoryAsync();
                if (patientHistory == null) return;

                var reviewRequest = CreateReviewRequest(formData, currentTimestamp, patientHistory);
                var reviewResponse = await _apiService.ReviewNotesAsync(reviewRequest);

                ProcessReviewResponse(reviewResponse);

                if (reviewResponse.IsSuccess && !string.IsNullOrEmpty(reviewResponse.RequestId))
                {
                    await PerformAutoEnhanceAsync(reviewResponse.RequestId);
                }
            }
        }
        private async Task PerformReviewWithNewApiAsync(FormData formData)
        {
            var patientHistory = await ParsePatientHistoryAsync();
            if (patientHistory == null) return;

            var newReviewRequest = CreateNewApiReviewRequest(formData, patientHistory);
            var newReviewResponse = await _apiService.ReviewNotesAsync(newReviewRequest);

            ProcessNewApiReviewResponse(newReviewResponse);

            if (newReviewResponse?.Status == "success" && !string.IsNullOrEmpty(newReviewResponse.ClaimReference))
            {
                await PerformEnhanceWithNewApiAsync(newReviewResponse.ClaimReference);
            }
        }

        private DoctorNotesReviewRequest CreateNewApiReviewRequest(FormData formData, JArray patientHistory)
        {
            return new DoctorNotesReviewRequest
            {
                InsuranceCompanyName = formData.InsuranceCompany,
                PatientIdentifier = formData.PatientIdentifier,
                ClaimReference = GenerateClaimReference(),
                Doctor = new DoctorInfo
                {
                    FullName = formData.DoctorName,
                    Email = WebConfigurationManager.AppSettings["DefaultDoctorEmail"] ?? "doctor@hospital.com",
                    Specialization = formData.DoctorSpecialization,
                    LicenseNumber = txtDoctorId?.Text ?? "DOC001"
                },
                DoctorNotes = formData.ClinicalNotes,
                History = patientHistory.ToString(Formatting.None),
                ModelName = "gemini-2.0-flash",
                Temperature = 0.3,
                MaxTokens = 16000,
                ClaimkitReviewRulesId = "doctornotes_review_criteria_expanded"
            };
        }

        private async Task PerformEnhanceWithNewApiAsync(string claimReference)
        {
            _requestId = claimReference;
            ViewState["RequestId"] = _requestId;

            var enhanceRequest = CreateNewApiEnhanceRequest(claimReference);
            var enhanceResponse = await _apiService.EnhanceNotesAsync(enhanceRequest);

            ProcessNewApiEnhanceResponse(enhanceResponse);
        }

        private DoctorNotesEnhanceRequest CreateNewApiEnhanceRequest(string claimReference)
        {
            return new DoctorNotesEnhanceRequest
            {
                ClaimReference = claimReference,
                ModelName = "gemini-2.0-flash",
                Temperature = 0.3,
                MaxTokens = 16000,
                ClaimkitEnhanceRulesId = "doctornotes_enhance_criteria_expanded"
            };
        }

        private string GenerateClaimReference()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssff");
            var encounter = txtEncounterId?.Text ?? "UNK";
            var hospital = ConfigurationService.HospitalId.ToString();

            return $"CR_{hospital}_{encounter}_{timestamp}";
        }

        private void ProcessNewApiReviewResponse(DoctorNotesReviewResponse response)
        {
            if (response?.Status != "success")
            {
                _uiManager.ShowError($"The clinical notes review could not be completed: {response?.Message?.En ?? "Unknown error"}");
                return;
            }

            _requestId = response.ClaimReference;
            if (string.IsNullOrEmpty(_requestId))
            {
                _uiManager.ShowError("Claim reference not returned from the medical records system.");
                return;
            }

            UpdateRequestIdInClient(_requestId);
            DisplayNewApiReviewResults(response);
            ShowReviewPanels();

            _logger.LogUserAction("New API Review Response Processed", $"Claim Reference: {_requestId}, Status: {response.Result?.Status}");
        }

        private void ProcessNewApiEnhanceResponse(DoctorNotesEnhanceResponse response)
        {
            if (response?.Status != "success")
            {
                _uiManager.ShowError($"The clinical notes enhancement could not be completed: {response?.Message?.En ?? "Unknown error"}");
                return;
            }

            if (response.Result?.Output?.Data?.EnhancedNotes == null)
            {
                _uiManager.ShowError("No enhanced clinical notes were found in the response.");
                return;
            }

            try
            {
                var enhancedNotesObj = ParseEnhancedNotesData(response.Result.Output.Data.EnhancedNotes);
                StoreEnhancedNotesData(enhancedNotesObj);
                DisplayEnhancedNotes(enhancedNotesObj);

                _logger.LogUserAction("New API Enhanced Notes Displayed", $"Claim Reference: {_requestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing enhanced notes from new API", ex);
                _uiManager.ShowError("Could not process enhanced notes. Please try again.");
            }
        }

        private void DisplayNewApiReviewResults(DoctorNotesReviewResponse response)
        {
            if (lblStatus != null)
                lblStatus.Text = $"<div class='success'>Status: {response.Result?.Status ?? "Review completed"}</div>";

            if (lblRequestId != null)
                lblRequestId.Text = $"<div>Claim Reference: {_requestId}</div>";

            if (response.Result?.Output?.Data?.TotalReviewFeedbackJsonList?.Any() == true && rptReviewCategories != null)
            {
                var reviewCategories = ConvertNewApiToReviewCategories(response.Result.Output.Data.TotalReviewFeedbackJsonList);
                rptReviewCategories.DataSource = reviewCategories;
                rptReviewCategories.DataBind();
            }
        }

        private List<ReviewCategory> ConvertNewApiToReviewCategories(List<ReviewFeedbackItem> feedbackList)
        {
            var categories = new List<ReviewCategory>();

            foreach (var feedback in feedbackList)
            {
                foreach (var feedbackDetail in feedback.Feedback)
                {
                    foreach (var section in feedbackDetail.Value)
                    {
                        categories.Add(new ReviewCategory
                        {
                            Category = feedback.Category,
                            Status = section.Value.Result,
                            Reason = section.Value.Reasoning
                        });
                    }
                }
            }

            return categories;
        }
        private ReviewRequest CreateReviewRequest(FormData formData, long timestamp, JArray patientHistory)
        {
            return new ReviewRequest
            {
                HospitalId = ConfigurationService.HospitalId,
                ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                HospitalPatientId = formData.PatientIdentifier,
                DoctorNotes = formData.ClinicalNotes,
                InsuranceCompany = formData.InsuranceCompany,
                PolicyBand = formData.PolicyBand,
                PolicyId = formData.PolicyId,
                PatientCheckinTime = timestamp,
                DoctorName = formData.DoctorName,
                DoctorSpecialization = formData.DoctorSpecialization,
                HospitalDoctorId = txtDoctorId.Text,
                PatientHistory = patientHistory
            };
        }

        private async Task PerformAutoEnhanceAsync(string requestId)
        {
            _requestId = requestId;
            ViewState["RequestId"] = _requestId;

            var enhanceRequest = new EnhanceRequest
            {
                HospitalId = ConfigurationService.HospitalId,
                ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                RequestId = _requestId
            };

            var enhanceResponse = await _apiService.EnhanceNotesAsync(enhanceRequest);
            ProcessEnhanceResponse(enhanceResponse);
        }

        private void ProcessReviewResponse(ReviewResponse response)
        {
            if (response?.IsSuccess != true)
            {
                _uiManager.ShowError($"The clinical notes review could not be completed: {response?.Message ?? "Unknown error"}");
                return;
            }

            _requestId = response.RequestId;
            if (string.IsNullOrEmpty(_requestId))
            {
                _uiManager.ShowError("Request ID not returned from the medical records system.");
                return;
            }

            UpdateRequestIdInClient(_requestId);
            DisplayReviewResults(response);
            ShowReviewPanels();

            _logger.LogUserAction("Review Response Processed", $"Request ID: {_requestId}, Status: {response.Message}");
        }

        private void ProcessEnhanceResponse(EnhanceResponse response)
        {
            if (response?.IsSuccess != true)
            {
                _uiManager.ShowError($"The clinical notes enhancement could not be completed: {response?.Message ?? "Unknown error"}");
                return;
            }

            if (response.Data?.EnhancedNotes == null)
            {
                _uiManager.ShowError("No enhanced clinical notes were found in the response.");
                return;
            }

            try
            {
                var enhancedNotesObj = ParseEnhancedNotesData(response.Data.EnhancedNotes);
                StoreEnhancedNotesData(enhancedNotesObj);
                DisplayEnhancedNotes(enhancedNotesObj);

                _logger.LogUserAction("Enhanced Notes Displayed", $"Request ID: {_requestId}, Sections: {enhancedNotesObj["sections"]?.Count() ?? 0}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing enhanced notes", ex);
                _uiManager.ShowError("Could not process enhanced notes. Please try again.");
            }
        }

        private void ProcessApprovedNotes(List<string> selectedSections)
        {
            ViewState["SelectedSections"] = JsonConvert.SerializeObject(selectedSections);

            if (Session["SavedFinalNotes"] != null)
            {
                txtFinalNotes.Text = Session["SavedFinalNotes"].ToString();
                Session.Remove("SavedFinalNotes");
            }
            else
            {
                PrepareAndShowFinalNotes(selectedSections);
            }

            if (pnlFinalNotes != null) pnlFinalNotes.Visible = true;
            if (pnlEnhancedNotes != null) pnlEnhancedNotes.Visible = false;

            _logger.LogUserAction("Approved Enhanced Notes", $"Request ID: {_requestId}, Section Count: {selectedSections.Count}");
        }

        private void PrepareAndShowFinalNotes(List<string> selectedSectionIds)
        {
            try
            {
                var enhancedNotes = GetEnhancedNotesData();
                if (enhancedNotes == null)
                {
                    _uiManager.ShowError("Could not retrieve enhanced notes data. Please try enhancing your notes again.");
                    return;
                }

                var finalNotesContent = BuildFinalNotesContent(enhancedNotes, selectedSectionIds);
                if (txtFinalNotes != null) txtFinalNotes.Text = finalNotesContent;

                _logger.LogUserAction("Final Notes Prepared", $"Request ID: {_requestId}, Selected Sections: {string.Join(", ", selectedSectionIds)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error preparing final notes", ex);
                _uiManager.ShowError("There was an issue preparing the final clinical notes. Please try again.");
            }
        }
        #endregion

        #region Helper Methods
        private async Task<JArray> ParsePatientHistoryAsync()
        {
            if (string.IsNullOrWhiteSpace(txtPatientHistory.Text))
            {
                return new JArray();
            }

            try
            {
                return await Task.FromResult(JArray.Parse(txtPatientHistory.Text));
            }
            catch (JsonException)
            {
                try
                {
                    return await ParseHumanReadableHistoryAsync(txtPatientHistory.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Patient history parsing error", ex, txtPatientHistory.Text);
                    _uiManager.ShowError("The patient history format is not recognized. Please check your entry and try again.");
                    return null;
                }
            }
        }

        private async Task<JArray> ParseHumanReadableHistoryAsync(string historyText)
        {
            return await Task.Run(() =>
            {
                var historyArray = new JArray();
                // Simple parsing logic - you can enhance this
                var lines = historyText.Split('\n');
                foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    historyArray.Add(new JObject
                    {
                        ["date"] = DateTime.Now.ToString("MM/dd/yyyy"),
                        ["doctor"] = "System",
                        ["diagnosis"] = line.Trim(),
                        ["treatment"] = "As documented"
                    });
                }
                return historyArray;
            });
        }

        private long GetCurrentUnixTimestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private void UpdateRequestIdInClient(string requestId)
        {
            ViewState["RequestId"] = requestId;
            if (hdnRequestId != null) hdnRequestId.Value = requestId;

            ScriptManager.RegisterStartupScript(this, GetType(), "SetRequestId",
                $"if(window.setCurrentRequestId) window.setCurrentRequestId('{requestId}'); else currentRequestId = '{requestId}';", true);
        }

        private void DisplayReviewResults(ReviewResponse response)
        {
            if (lblStatus != null)
                lblStatus.Text = $"<div class='success'>Status: {FormatStatusMessage(response.Message)}</div>";

            if (lblRequestId != null)
                lblRequestId.Text = $"<div>Request ID: {_requestId}</div>";

            if (response.Review?.Any() == true && rptReviewCategories != null)
            {
                rptReviewCategories.DataSource = response.Review;
                rptReviewCategories.DataBind();
            }
        }

        private void ShowReviewPanels()
        {
            if (pnlReviewResults != null) pnlReviewResults.Visible = true;
            if (pnlActionButtons != null) pnlActionButtons.Visible = true;
        }

        private JObject ParseEnhancedNotesData(object enhancedNotes)
        {
            if (enhancedNotes is JObject jobj)
            {
                return jobj;
            }
            else if (enhancedNotes is string str)
            {
                return JObject.Parse(str);
            }
            else
            {
                string jsonStr = JsonConvert.SerializeObject(enhancedNotes);
                return JObject.Parse(jsonStr);
            }
        }

        private void StoreEnhancedNotesData(JObject enhancedNotesObj)
        {
            var jsonStr = enhancedNotesObj.ToString(Formatting.None);
            ViewState["EnhancedNotesData"] = jsonStr;
            Session["EnhancedNotesData"] = jsonStr;

            if (hdnEnhancedNotesData != null)
            {
                hdnEnhancedNotesData.Value = jsonStr;
            }
        }

        private void DisplayEnhancedNotes(JObject enhancedNotesObj)
        {
            var serializedData = enhancedNotesObj.ToString(Formatting.None);
            var htmlContent = $"<div id='enhancedNotesDataContainer' style='display:none;' " +
                            $"data-json='{System.Web.HttpUtility.HtmlAttributeEncode(serializedData)}'></div>" +
                            $"<div id='enhancedNotesDisplayContainer'></div>";

            if (litEnhancedNotes != null) litEnhancedNotes.Text = htmlContent;
            if (pnlEnhancedNotes != null) pnlEnhancedNotes.Visible = true;

            hdnShowModal.Value = "showEnhancedNotesModal";
        }

        private JObject GetEnhancedNotesData()
        {
            // Try multiple sources for enhanced notes data
            var sources = new Func<string>[]
            {
                () => hdnEnhancedNotesData?.Value,
                () => ViewState["EnhancedNotesData"] as string,
                () => Session["EnhancedNotesData"] as string
            };

            foreach (var source in sources)
            {
                try
                {
                    var jsonData = source();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        return JObject.Parse(jsonData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error parsing enhanced notes from source", ex);
                }
            }

            return null;
        }

        private string BuildFinalNotesContent(JObject enhancedNotes, List<string> selectedSectionIds)
        {
            var finalNotes = new StringBuilder();

            // Add header
            finalNotes.AppendLine("# Patient Medical Record");
            finalNotes.AppendLine($"Date of Documentation: {DateTime.Now:yyyy-MM-dd}");

            var patientId = GetPatientIdentifier();
            if (patientId != "Unknown")
            {
                finalNotes.AppendLine($"Patient ID: {patientId}");
            }

            // Add insurance and clinician info
            AddInsuranceInfo(finalNotes);
            AddClinicianInfo(finalNotes);

            // Process selected sections
            if (enhancedNotes["sections"]?.Type == JTokenType.Object)
            {
                var sections = (JObject)enhancedNotes["sections"];
                foreach (var sectionId in selectedSectionIds)
                {
                    if (sections[sectionId] != null)
                    {
                        finalNotes.Append(FormatSectionForFinalNotes(sectionId, sections[sectionId]));
                    }
                }
            }

            // Add footer
            finalNotes.AppendLine("---");
            finalNotes.AppendLine("Generated by ClaimKit Medical Documentation Assistant");
            if (!string.IsNullOrEmpty(_requestId))
            {
                finalNotes.AppendLine($"Request ID: {_requestId}");
            }

            return finalNotes.ToString();
        }

        private void AddInsuranceInfo(StringBuilder finalNotes)
        {
            if (!string.IsNullOrEmpty(txtInsuranceCompany?.Text))
            {
                var insuranceInfo = txtInsuranceCompany.Text;
                if (!string.IsNullOrEmpty(txtPolicyId?.Text))
                {
                    insuranceInfo += $" (Policy: {txtPolicyId.Text})";
                }
                finalNotes.AppendLine($"Insurance: {insuranceInfo}");
            }
        }

        private void AddClinicianInfo(StringBuilder finalNotes)
        {
            finalNotes.AppendLine();
            finalNotes.AppendLine($"Clinician: {txtDoctorName?.Text}");

            if (!string.IsNullOrEmpty(txtDoctorSpecialization?.Text))
            {
                finalNotes.AppendLine($"Specialization: {txtDoctorSpecialization.Text}");
            }

            if (!string.IsNullOrEmpty(txtDoctorId?.Text))
            {
                finalNotes.AppendLine($"Clinician ID: {txtDoctorId.Text}");
            }

            finalNotes.AppendLine();
        }

        private string FormatSectionForFinalNotes(string sectionId, JToken sectionData)
        {
            var result = new StringBuilder();

            try
            {
                var title = FormatSectionTitle(sectionId);
                if (sectionData.Type == JTokenType.Object && sectionData["title"] != null)
                {
                    title = sectionData["title"].ToString();
                }

                result.AppendLine($"## {title}");
                result.AppendLine();

                // Simple formatting for now - you can enhance this
                result.AppendLine(sectionData.ToString());
                result.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error formatting section '{sectionId}'", ex);
                result.AppendLine($"**Error processing section: {ex.Message}**");
                result.AppendLine();
            }

            return result.ToString();
        }

        private string FormatSectionTitle(string section)
        {
            if (string.IsNullOrEmpty(section)) return string.Empty;

            var result = section.Replace('_', ' ');
            result = System.Text.RegularExpressions.Regex.Replace(result, "([A-Z])", " $1");

            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1);
            }

            return result.Trim();
        }

        private string FormatStatusMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "Review completed";
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(message.Replace('_', ' '));
        }

        private string FormatReviewCategory(ReviewCategory category)
        {
            var content = new StringBuilder();
            var displayName = FormatCategoryName(category.Category ?? "Unknown Category");
            var statusClass = GetStatusClass(category.Status);

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
            return content.ToString();
        }

        private string FormatCategoryName(string categoryName)
        {
            if (categoryName.Contains("_") && char.IsDigit(categoryName[0]))
            {
                categoryName = categoryName.Substring(categoryName.IndexOf('_') + 1);
            }

            categoryName = categoryName.Replace('_', ' ');
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryName);
        }

        private string GetStatusClass(string status)
        {
            if (string.IsNullOrEmpty(status)) return "status-neutral";

            var lowerStatus = status.ToLower();
            return lowerStatus.Contains("inconsistent") || lowerStatus.Contains("gap detected") || lowerStatus.Contains("non-compliant")
                ? "status-inconsistent"
                : lowerStatus.Contains("consistent") || lowerStatus.Contains("complete") || lowerStatus.Contains("compliant")
                ? "status-consistent"
                : "status-neutral";
        }

        private string SaveNotesToFile()
        {
            var savedNotesDir = Path.Combine(Server.MapPath("~/"), "Saved Notes");
            Directory.CreateDirectory(savedNotesDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var patientId = string.IsNullOrEmpty(txtEncounterId.Text) ? "unknown" : txtEncounterId.Text;
            var doctorId = string.IsNullOrEmpty(txtDoctorId.Text) ? "unknown" : txtDoctorId.Text;
            var filename = $"Notes_{patientId}_{doctorId}_{timestamp}.txt";
            var filePath = Path.Combine(savedNotesDir, filename);

            File.WriteAllText(filePath, txtFinalNotes.Text);
            return filePath;
        }

        private void ClearEmrData()
        {
            if (txtPatientId != null) txtPatientId.Text = string.Empty;
            txtDoctorNotes.Text = string.Empty;
            txtPatientHistory.Text = string.Empty;

            if (ViewState["EmrDataLoaded"] as bool? == true)
            {
                txtDoctorName.Text = string.Empty;
                txtDoctorSpecialization.Text = string.Empty;
                txtDoctorId.Text = string.Empty;
            }

            Session.Remove("LoadedEmrData");
            ViewState["EmrDataLoaded"] = false;
        }
        #endregion

        #region Web Methods
        [System.Web.Services.WebMethod]
        public static string CheckEnhancementStatus(string requestId)
        {
            try
            {
                var logger = LoggingService.Instance;
                logger.LogUserAction("Enhancement Status Check", $"Request ID: {requestId}");

                var httpContext = System.Web.HttpContext.Current;
                if (httpContext?.Session?["EnhancedNotesData"] != null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        status = "complete",
                        data = httpContext.Session["EnhancedNotesData"].ToString(),
                        message = "Enhancement completed successfully"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    status = "inprogress",
                    message = "Enhancement is still in progress"
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error checking enhancement status", ex);
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    message = "Error checking enhancement status: " + ex.Message
                });
            }
        }

        [System.Web.Services.WebMethod]
        public static string ValidateFormData(string clinicalNotes, string insuranceCompany, string policyBand, string doctorName)
        {
            try
            {
                var validationService = new ValidationService(LoggingService.Instance);
                var formData = new FormData
                {
                    ClinicalNotes = clinicalNotes,
                    InsuranceCompany = insuranceCompany,
                    PolicyBand = policyBand,
                    DoctorName = doctorName
                };

                var result = validationService.ValidateClaimKitRequiredFields(formData);

                return JsonConvert.SerializeObject(new
                {
                    isValid = result.IsValid,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    isValid = false,
                    errors = new string[] { "Validation error: " + ex.Message }
                });
            }
        }
        #endregion

        #region EMR Integration Methods
        private void ProcessEmrIntegrationResult(EmrIntegrationResult result)
        {
            try
            {
                // Populate form fields from mapped data using your existing model
                PopulateFieldsFromEmrMapping(result.MappedData);

                // Store in session for later use
                Session["LoadedEmrData"] = result.EmrData;
                Session["EmrMapping"] = result.MappedData;
                ViewState["EmrDataLoaded"] = true;

                // Show the clear button
                if (btnClearEmrData != null) btnClearEmrData.Visible = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing EMR integration result", ex);
                _uiManager.ShowError("EMR data was loaded but there was an issue displaying it. Please check the logs.");
            }
        }

        private void PopulateFieldsFromEmrMapping(ClaimKitEmrMapping mapping)
        {
            if (mapping == null) return;

            try
            {
                // Populate form fields using your existing model
                if (txtPatientId != null) txtPatientId.Text = mapping.HospitalPatientId ?? "";
                if (txtDoctorName != null) txtDoctorName.Text = mapping.DoctorName ?? "";
                if (txtDoctorSpecialization != null) txtDoctorSpecialization.Text = mapping.DoctorSpecialization ?? "";
                if (txtDoctorId != null) txtDoctorId.Text = mapping.HospitalDoctorId ?? "";

                if (!string.IsNullOrEmpty(mapping.InsuranceCompany))
                {
                    if (txtInsuranceCompany != null) txtInsuranceCompany.Text = mapping.InsuranceCompany;
                    SetPolicyBandFromMapping(mapping.PolicyBand);
                    if (txtPolicyId != null) txtPolicyId.Text = mapping.PolicyId ?? "";
                }

                // Update clinical notes if available
                if (!string.IsNullOrEmpty(mapping.DoctorNotes))
                {
                    if (string.IsNullOrWhiteSpace(txtDoctorNotes?.Text))
                    {
                        txtDoctorNotes.Text = mapping.DoctorNotes;
                    }
                    else
                    {
                        txtDoctorNotes.Text += $"\n\n{new string('=', 60)}\n";
                        txtDoctorNotes.Text += "EMR DATA INTEGRATION\n";
                        txtDoctorNotes.Text += $"{new string('=', 60)}\n\n";
                        txtDoctorNotes.Text += mapping.DoctorNotes;
                    }
                }

                // Update patient history if available
                if (mapping.PatientHistory?.Count > 0)
                {
                    if (txtPatientHistory != null)
                    {
                        txtPatientHistory.Text = mapping.PatientHistory.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error populating fields from EMR mapping", ex);
            }
        }

        private void SetPolicyBandFromMapping(string policyBand)
        {
            if (string.IsNullOrEmpty(policyBand)) return;

            try
            {
                if (ddlPolicyBand?.Items.FindByValue(policyBand) != null)
                {
                    ddlPolicyBand.SelectedValue = policyBand;
                }
                else if (txtPolicyBand != null)
                {
                    txtPolicyBand.Text = policyBand;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error setting policy band", ex, policyBand);
            }
        }
        #endregion

        #region UI Helper Methods
        private void ResetResultPanels()
        {
            var panels = new Panel[] { pnlReviewResults, pnlEnhancedNotes, pnlFinalNotes, pnlError, pnlConfirmation };
            foreach (var panel in panels.Where(p => p != null))
            {
                panel.Visible = false;
            }
        }

        private void ResetWorkflowPanels()
        {
            ResetResultPanels();
            _requestId = null;
            ViewState["RequestId"] = null;
        }

        private void UpdateOriginalNotes()
        {
            txtDoctorNotes.Text = txtFinalNotes.Text;
        }

        private void ShowEnhancedNotesModal()
        {
            if (pnlEnhancedNotes != null) pnlEnhancedNotes.Visible = true;
            hdnShowModal.Value = "showEnhancedNotesModal";
            if (pnlFinalNotes != null) pnlFinalNotes.Visible = false;
        }

        // Public property for services to access request ID
        public string RequestId
        {
            get { return _requestId; }
            set
            {
                _requestId = value;
                ViewState["RequestId"] = value;
                if (hdnRequestId != null) hdnRequestId.Value = value;
            }
        }
        #endregion
    }
}