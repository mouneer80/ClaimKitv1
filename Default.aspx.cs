using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        // Store the request ID for sequential workflows
        private string _requestId;

        // Store checkout time for generate claim
        private long _checkoutTime;

        public Default()
        {
            // Simple service initialization - could use IoC container in a more advanced implementation
            _apiService = new ClaimKitApiService();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Set default values from configuration
                txtPolicyId.Text = WebConfigurationManager.AppSettings["DefaultPolicyId"];
                txtPatientHistory.Text = WebConfigurationManager.AppSettings["DefaultPatientHistory"];
            }

            // Retrieve requestId from ViewState if available
            if (ViewState["RequestId"] != null)
            {
                _requestId = ViewState["RequestId"].ToString();
            }
        }

        protected void btnReviewNotes_Click(object sender, EventArgs e)
        {
            // Hide previous results
            ResetResultPanels();

            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtDoctorNotes.Text))
            {
                DisplayError("Doctor notes are required.");
                return;
            }

            // Register async task
            RegisterAsyncTask(new PageAsyncTask(PerformReviewAsync));

            // Automatically show the review results popup
            ScriptManager.RegisterStartupScript(this, GetType(), "ShowReviewResults", "window.showReviewResultsModal();", true);
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

                // Call API service
                var response = await _apiService.ReviewNotesAsync(reviewRequest);

                // Process response
                ProcessReviewResponse(response);
            }
            catch (Exception ex)
            {
                DisplayError($"Error: {ex.Message}");
            }
        }

        protected void btnEnhanceNotes_Click(object sender, EventArgs e)
        {
            // Hide previous results
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;
            //lblError.Visible = false;

            // Register async task
            RegisterAsyncTask(new PageAsyncTask(PerformEnhanceAsync));
        }

        private async Task PerformEnhanceAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("You must review notes first to get a request ID.");
                    return;
                }

                // Create enhance request
                var enhanceRequest = new EnhanceRequest
                {
                    HospitalId = ConfigurationService.HospitalId,
                    ClaimKitApiKey = ConfigurationService.ClaimKitApiKey,
                    RequestId = _requestId
                };

                // Call API service
                var response = await _apiService.EnhanceNotesAsync(enhanceRequest);

                // Process response
                ProcessEnhanceResponse(response);
            }
            catch (Exception ex)
            {
                DisplayError($"Error: {ex.Message}");
            }
        }

        protected void btnGenerateClaim_Click(object sender, EventArgs e)
        {
            // Hide previous results
            pnlGeneratedClaim.Visible = false;
            //lblError.Visible = false;

            // Register async task
            RegisterAsyncTask(new PageAsyncTask(PerformGenerateClaimAsync));
        }

        private async Task PerformGenerateClaimAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("You must review notes first to get a request ID.");
                    return;
                }

                // Get checkout time from ViewState
                long checkoutTime = (long)ViewState["CheckoutTime"];

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

                // Call API service
                var response = await _apiService.GenerateClaimAsync(generateClaimRequest);

                // Process response
                ProcessGenerateClaimResponse(response);
            }
            catch (Exception ex)
            {
                DisplayError($"Error: {ex.Message}");
            }
        }

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
                        // Use strongly-typed ReviewCategory object
                        content.AppendLine($"<div class=\"category-name\">{category.Category ?? "Category not available"}</div>");

                        if (!string.IsNullOrEmpty(category.Status))
                        {
                            content.AppendLine($"<div class=\"category-status\">Status: {category.Status}</div>");
                        }

                        if (!string.IsNullOrEmpty(category.Reason))
                        {
                            content.AppendLine($"<div class=\"category-reason\">{category.Reason}</div>");
                        }
                    }
                    else
                    {
                        // Fallback if it's not a ReviewCategory
                        content.AppendLine("<div class=\"category-name\">Unknown category format</div>");
                        content.AppendLine($"<div class=\"category-type\">Type: {dataItem?.GetType().Name ?? "null"}</div>");
                    }
                }
                catch (Exception ex)
                {
                    content.AppendLine($"<div class=\"error\">Error displaying category: {ex.Message}</div>");
                }

                // Set the content
                litCategoryContent.Text = content.ToString();
            }
        }

        #region Response Processing Methods

        private void ProcessReviewResponse(ReviewResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the API.");
                return;
            }

            // Debug: Display raw response for inspection
            //DisplayError($"Debug - Raw API Response: {response.RawResponse}");
            ProcessRawJsonResponse(response.RawResponse);

            if (response.IsSuccess)
            {
                // Store request ID for future calls
                _requestId = response.RequestId;
                if (string.IsNullOrEmpty(_requestId))
                {
                    DisplayError("Request ID not returned from the API.");
                    return;
                }

                ViewState["RequestId"] = _requestId;

                // Display success message
                lblStatus.Text = $"<div class='success'>Status: {response.Message}</div>";
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
                    lblStatus.Text += "<div>No review data available</div>";
                }

                // Show results panel and enable enhance/generate buttons
                pnlReviewResults.Visible = true;
                btnEnhanceNotes.Visible = true;
                btnGenerateClaim.Visible = true;
            }
            else
            {
                // Display error message
                DisplayError($"Error: {response.Message ?? "Unknown error occurred"}");
            }
        }

        private void ProcessEnhanceResponse(EnhanceResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the API.");
                return;
            }

            if (response.IsSuccess)
            {
                if (response.Data != null && response.Data.EnhancedNotes != null)
                {
                    // Display enhanced notes
                    litEnhancedNotes.Text = JsonConvert.SerializeObject(response.Data.EnhancedNotes, Formatting.Indented);
                    pnlEnhancedNotes.Visible = true;
                }
                else
                {
                    DisplayError("No enhanced notes found in the response.");
                }
            }
            else
            {
                // Display error message
                DisplayError($"Error: {response.Message ?? "Unknown error occurred"}");
            }
        }

        private void ProcessGenerateClaimResponse(GenerateClaimResponse response)
        {
            if (response == null)
            {
                DisplayError("No response received from the API.");
                return;
            }

            if (response.IsSuccess)
            {
                if (response.Data != null && response.Data.GeneratedClaim != null)
                {
                    // Display generated claim
                    litGeneratedClaim.Text = JsonConvert.SerializeObject(response.Data.GeneratedClaim, Formatting.Indented);
                    pnlGeneratedClaim.Visible = true;
                }
                else
                {
                    DisplayError("No generated claim found in the response.");
                }
            }
            else
            {
                // Display error message
                DisplayError($"Error: {response.Message ?? "Unknown error occurred"}");
            }
        }

        #endregion

        #region Helper Methods

        private void ResetResultPanels()
        {
            pnlReviewResults.Visible = false;
            pnlEnhancedNotes.Visible = false;
            pnlGeneratedClaim.Visible = false;
            //lblError.Visible = false;
        }

        protected void btnCloseError_Click(object sender, EventArgs e)
        {
            pnlError.Visible = false;
        }

        protected void btnCloseResponse_Click(object sender, EventArgs e)
        {
            pnlJsonResponse.Visible = false;
        }

        /// <summary>
        /// Enhanced error display method that formats JSON responses
        /// </summary>
        private void DisplayError(string errorMessage)
        {
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

                // IMPORTANT FIX: Directly insert pre-formatted JSON rather than using JavaScript
                preFormattedJson.InnerHtml = FormatJsonToHtml(jsonContent);
            }

            // Show the error panel
            pnlError.Visible = true;
        }

        /// <summary>
        /// Parses the raw JSON response and updates UI accordingly
        /// </summary>
        private void ProcessRawJsonResponse(string jsonResponse)
        {
            ProcessReviewResponse(jsonResponse);
            //try
            //{
            //    // Parse the JSON to determine if it's a success or error
            //    JObject responseObj = JObject.Parse(jsonResponse);

            //    string status = responseObj["status"]?.ToString();

            //    if (status == "success")
            //    {
            //        // Get request ID if available
            //        string requestId = responseObj["request_id"]?.ToString();
            //        if (!string.IsNullOrEmpty(requestId))
            //        {
            //            _requestId = requestId;
            //            ViewState["RequestId"] = _requestId;
            //        }

            //        // For JSON response display, we'll still show it in the error panel but with success styling
            //        lblErrorMessage.Text = "Successfully received response from server";
            //        lblErrorMessage.CssClass = "success-message";

            //        // Format and display the JSON
            //        pnlJsonContent.Visible = true;

            //        // IMPORTANT FIX: Directly insert pre-formatted JSON rather than using JavaScript
            //        preFormattedJson.InnerHtml = FormatJsonToHtml(jsonResponse);

            //        // Show the panel
            //        pnlError.Visible = true;
            //    }
            //    else
            //    {
            //        // It's an error response, use the standard error display
            //        DisplayError(jsonResponse);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // If parsing fails, just display as a regular error
            //    DisplayError($"Error parsing response: {ex.Message}\n\nRaw response: {jsonResponse}");
            //}
        }

        /// <summary>
        /// Formats JSON string with proper indentation and HTML syntax highlighting
        /// </summary>
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

        /// <summary>
        /// Attempts to extract JSON content from a string
        /// </summary>
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

        /// <summary>
        /// Cleans up an error message by removing JSON content and debug prefixes
        /// </summary>
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

        /// <summary>
        /// Formats review feedback content, handling both direct JSON objects and JSON strings
        /// </summary>
        private string FormatFeedbackContent(JToken feedbackContent)
        {
            if (feedbackContent == null)
                return "<div class='no-data'>No feedback data available</div>";

            try
            {
                // Case 1: Feedback is a JSON string that needs to be parsed
                if (feedbackContent.Type == JTokenType.String)
                {
                    string feedbackStr = feedbackContent.ToString();

                    // Replace escaped newlines with actual newlines before parsing
                    feedbackStr = feedbackStr.Replace("\\n", "\n");

                    // Try to parse the string as JSON
                    try
                    {
                        JToken parsedFeedback = JToken.Parse(feedbackStr);
                        return FormatFeedbackJson(parsedFeedback);
                    }
                    catch
                    {
                        // If parsing fails, just display the raw string
                        return $"<pre>{System.Web.HttpUtility.HtmlEncode(feedbackStr)}</pre>";
                    }
                }
                // Case 2: Feedback is already a JSON object or array
                else if (feedbackContent.Type == JTokenType.Object || feedbackContent.Type == JTokenType.Array)
                {
                    return FormatFeedbackJson(feedbackContent);
                }
                // Case 3: Other data types
                else
                {
                    return $"<div class='feedback-raw'>{System.Web.HttpUtility.HtmlEncode(feedbackContent.ToString())}</div>";
                }
            }
            catch (Exception ex)
            {
                return $"<div class='error'>Error formatting feedback: {ex.Message}</div>";
            }
        }

        /// <summary>
        /// Process JSON review response into nicely formatted HTML
        /// </summary>
        private void ProcessReviewResponse(string jsonResponse)
        {
            try
            {
                // Parse the JSON
                JObject responseObj = JToken.Parse(jsonResponse) as JObject;
                if (responseObj == null)
                {
                    DisplayError("Invalid JSON response received");
                    return;
                }

                // Get status and basic info
                string status = responseObj["status"]?.ToString() ?? "unknown";
                string message = responseObj["message"]?.ToString() ?? "";
                string requestId = responseObj["request_id"]?.ToString() ?? "";

                // Store request ID if available
                if (!string.IsNullOrEmpty(requestId))
                {
                    _requestId = requestId;
                    ViewState["RequestId"] = _requestId;
                }

                // Get the review array
                JArray reviewArray = responseObj["review"] as JArray;

                // Start building the HTML for the formatted review
                StringBuilder reviewHtml = new StringBuilder();

                if (reviewArray != null && reviewArray.Count > 0)
                {
                    reviewHtml.AppendLine("<div class='review-container'>");

                    foreach (JObject category in reviewArray)
                    {
                        string categoryName = category["category"]?.ToString() ?? "Unknown Category";
                        JToken feedback = category["feedback"];

                        // Format category name for display
                        string displayName = FormatCategoryName(categoryName);

                        // Add category section
                        reviewHtml.AppendLine($"<div class='review-category'>");
                        reviewHtml.AppendLine($"<div class='category-header' onclick=\"toggleCategory('category_{categoryName.Replace(" ", "_")}')\">");
                        reviewHtml.AppendLine($"<span class='category-name'>{displayName}</span>");
                        reviewHtml.AppendLine($"<span class='toggle-icon'>+</span>");
                        reviewHtml.AppendLine($"</div>");

                        // Add category content (collapsed by default)
                        reviewHtml.AppendLine($"<div id='category_{categoryName.Replace(" ", "_")}' class='category-content'>");

                        // Format the feedback content
                        if (feedback != null)
                        {
                            reviewHtml.AppendLine(FormatFeedbackContent(feedback));
                        }
                        else
                        {
                            reviewHtml.AppendLine("<div class='no-data'>No feedback data available for this category</div>");
                        }

                        reviewHtml.AppendLine("</div>"); // End category-content
                        reviewHtml.AppendLine("</div>"); // End review-category
                    }

                    reviewHtml.AppendLine("</div>"); // End review-container

                    // Display the formatted review in a panel
                    divFormattedReview.InnerHtml = reviewHtml.ToString();
                    pnlFormattedReview.Visible = true;

                    // Also show the raw JSON for reference/debugging
                    litRawJson.Text = FormatJsonToHtml(jsonResponse);
                    pnlRawJson.Visible = true;

                    // Update status message
                    lblResponseStatus.Text = status.ToUpperInvariant();
                    lblResponseStatus.CssClass = $"status-label {(status == "success" ? "success" : "error")}";

                    lblResponseMessage.Text = $"Response Message: {FormatResponseMessage(message)}";
                    if (!string.IsNullOrEmpty(requestId))
                    {
                        lblRequestId.Text = $"Request ID: {requestId}";
                    }

                    // Show the response panel
                    pnlJsonResponse.Visible = true;
                }
                else
                {
                    // No review data found
                    DisplayError("No review data found in the response");
                }
            }
            catch (Exception ex)
            {
                // Handle parsing errors
                DisplayError($"Error processing JSON response: {ex.Message}\n\n{jsonResponse}");
            }
        }

        /// <summary>
        /// Formats a category name for display
        /// </summary>
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

        /// <summary>
        /// Formats the response message for display
        /// </summary>
        private string FormatResponseMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "No message provided";

            // Replace underscores with spaces and title case
            message = message.Replace('_', ' ');
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(message);
        }

        /// <summary>
        /// Formats a JSON object into HTML with tables for feedback items
        /// </summary>
        private string FormatFeedbackJson(JToken feedbackJson)
        {
            var html = new StringBuilder();

            try
            {
                // Handle different JSON structures
                if (feedbackJson is JObject rootObj)
                {
                    // Process each top-level step
                    foreach (var stepProp in rootObj.Properties())
                    {
                        string stepName = stepProp.Name;
                        JToken stepValue = stepProp.Value;

                        html.AppendLine($"<div class='feedback-step'>");
                        html.AppendLine($"<h4>{stepName}</h4>");

                        if (stepValue is JObject stepObj)
                        {
                            // Create a table for the checks
                            html.AppendLine("<table class='feedback-table'>");
                            html.AppendLine("<thead><tr><th>Check</th><th>Result</th><th>Reasoning</th></tr></thead>");
                            html.AppendLine("<tbody>");

                            foreach (var checkProp in stepObj.Properties())
                            {
                                string checkName = checkProp.Name;
                                JToken checkValue = checkProp.Value;

                                if (checkValue is JObject checkObj)
                                {
                                    string result = checkObj["result"]?.ToString() ?? "N/A";
                                    string reasoning = checkObj["reasoning"]?.ToString() ?? "No reasoning provided";

                                    // Determine status class based on result
                                    string statusClass = GetStatusClassFromResult(result);

                                    html.AppendLine("<tr>");
                                    html.AppendLine($"<td class='check-name'>{checkName}</td>");
                                    html.AppendLine($"<td class='check-result {statusClass}'>{result}</td>");
                                    html.AppendLine($"<td class='check-reasoning'>{reasoning}</td>");
                                    html.AppendLine("</tr>");
                                }
                                else
                                {
                                    // Handle non-object check values (unexpected format)
                                    html.AppendLine("<tr>");
                                    html.AppendLine($"<td class='check-name'>{checkName}</td>");
                                    html.AppendLine($"<td class='check-raw' colspan='2'>{System.Web.HttpUtility.HtmlEncode(checkValue.ToString())}</td>");
                                    html.AppendLine("</tr>");
                                }
                            }

                            html.AppendLine("</tbody></table>");
                        }
                        else
                        {
                            // Handle unexpected step value format
                            html.AppendLine($"<div class='step-raw'>{System.Web.HttpUtility.HtmlEncode(stepValue.ToString())}</div>");
                        }

                        html.AppendLine("</div>"); // end .feedback-step
                    }
                }
                else if (feedbackJson is JArray array)
                {
                    // Handle array of feedback items
                    html.AppendLine("<div class='feedback-array'>");

                    foreach (var item in array)
                    {
                        html.AppendLine("<div class='feedback-item'>");
                        html.AppendLine(FormatFeedbackJson(item));
                        html.AppendLine("</div>");
                    }

                    html.AppendLine("</div>");
                }
                else
                {
                    // Handle any other format
                    html.AppendLine($"<pre>{System.Web.HttpUtility.HtmlEncode(feedbackJson.ToString(Formatting.Indented))}</pre>");
                }
            }
            catch (Exception ex)
            {
                html.AppendLine($"<div class='error'>Error processing feedback: {ex.Message}</div>");
                html.AppendLine($"<pre>{System.Web.HttpUtility.HtmlEncode(feedbackJson.ToString(Formatting.Indented))}</pre>");
            }

            return html.ToString();
        }

        /// <summary>
        /// Determines the CSS class for a result based on its value
        /// </summary>
        private string GetStatusClassFromResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "neutral";

            string lowerResult = result.ToLower();

            if (lowerResult.Contains("consistent") && !lowerResult.Contains("inconsistent") ||
                lowerResult == "necessary" ||
                lowerResult == "relevant" ||
                lowerResult == "compliant")
            {
                return "positive";
            }
            else if (lowerResult.Contains("inconsistent") ||
                     lowerResult == "unnecessary" ||
                     lowerResult == "irrelevant" ||
                     lowerResult.Contains("non-compliant") ||
                     lowerResult == "gaps detected")
            {
                return "negative";
            }

            return "neutral";
        }

        private long GetCurrentUnixTimestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private JArray ParsePatientHistory()
        {
            try
            {
                return JArray.Parse(txtPatientHistory.Text);
            }
            catch (Exception ex)
            {
                DisplayError($"Patient history must be a valid JSON array. Error: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}