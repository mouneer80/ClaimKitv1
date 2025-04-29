using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Helpers
{
    public class EnhancedNotesFormatter
    {
        /// <summary>
        /// Formats the enhanced notes JSON into a structured HTML display
        /// </summary>
        /// <param name="enhancedNotesJson">The enhanced notes JSON object</param>
        /// <returns>Formatted HTML string</returns>
        public static string FormatEnhancedNotes(JObject enhancedNotesJson)
        {
            if (enhancedNotesJson == null)
                return "No enhanced notes data available.";

            var html = new StringBuilder();
            try
            {
                // Start building the medical record
                html.AppendLine("<div class=\"enhanced-notes-container\">");

                // Add title if available
                if (enhancedNotesJson["title"] != null)
                {
                    html.AppendLine($"<h2 class=\"medical-record-title\">{enhancedNotesJson["title"]}</h2>");
                }

                // Process sections
                if (enhancedNotesJson["sections"] != null && enhancedNotesJson["sections"].Type == JTokenType.Object)
                {
                    var sections = (JObject)enhancedNotesJson["sections"];
                    foreach (var section in sections.Properties())
                    {
                        html.AppendLine(FormatSection(section.Name, section.Value));
                    }
                }

                html.AppendLine("</div>");

                // Add selection controls
                html.AppendLine("<div class=\"selection-controls\">");
                html.AppendLine("  <div class=\"selection-header\">");
                html.AppendLine("    <h3>Select Notes to Include</h3>");
                html.AppendLine("    <p>Check the sections you wish to include in your final documentation.</p>");
                html.AppendLine("  </div>");
                html.AppendLine("  <div class=\"selection-actions\">");
                html.AppendLine("    <button type=\"button\" id=\"btnSelectAll\" class=\"btn btn-secondary btn-sm\" onclick=\"selectAllNotes()\">Select All</button>");
                html.AppendLine("    <button type=\"button\" id=\"btnDeselectAll\" class=\"btn btn-secondary btn-sm\" onclick=\"deselectAllNotes()\">Deselect All</button>");
                //html.AppendLine("    <button type=\"button\" id=\"btnApproveNotes\" class=\"btn btn-primary\" onclick=\"approveSelectedNotes()\">Approve Selected Notes</button>");
                html.AppendLine("  </div>");
                html.AppendLine("</div>");

                // Add JavaScript for selection functionality
                html.AppendLine("<script type=\"text/javascript\">");
                html.AppendLine("var selectedNotes = [];");
                html.AppendLine("");
                html.AppendLine("function toggleNoteSelection(noteId) {");
                html.AppendLine("  var checkbox = document.getElementById('chk-' + noteId);");
                html.AppendLine("  var noteElement = document.getElementById('note-' + noteId);");
                html.AppendLine("");
                html.AppendLine("  if (checkbox.checked) {");
                html.AppendLine("    noteElement.classList.add('selected');");
                html.AppendLine("    if (selectedNotes.indexOf(noteId) === -1) {");
                html.AppendLine("      selectedNotes.push(noteId);");
                html.AppendLine("    }");
                html.AppendLine("  } else {");
                html.AppendLine("    noteElement.classList.remove('selected');");
                html.AppendLine("    var index = selectedNotes.indexOf(noteId);");
                html.AppendLine("    if (index !== -1) {");
                html.AppendLine("      selectedNotes.splice(index, 1);");
                html.AppendLine("    }");
                html.AppendLine("  }");
                html.AppendLine("}");
                html.AppendLine("");
                html.AppendLine("function selectAllNotes() {");
                html.AppendLine("  var checkboxes = document.querySelectorAll('.note-checkbox');");
                html.AppendLine("  checkboxes.forEach(function(checkbox) {");
                html.AppendLine("    checkbox.checked = true;");
                html.AppendLine("    var noteId = checkbox.id.replace('chk-', '');");
                html.AppendLine("    var noteElement = document.getElementById('note-' + noteId);");
                html.AppendLine("    noteElement.classList.add('selected');");
                html.AppendLine("    if (selectedNotes.indexOf(noteId) === -1) {");
                html.AppendLine("      selectedNotes.push(noteId);");
                html.AppendLine("    }");
                html.AppendLine("  });");
                html.AppendLine("}");
                html.AppendLine("");
                html.AppendLine("function deselectAllNotes() {");
                html.AppendLine("  var checkboxes = document.querySelectorAll('.note-checkbox');");
                html.AppendLine("  checkboxes.forEach(function(checkbox) {");
                html.AppendLine("    checkbox.checked = false;");
                html.AppendLine("    var noteId = checkbox.id.replace('chk-', '');");
                html.AppendLine("    var noteElement = document.getElementById('note-' + noteId);");
                html.AppendLine("    noteElement.classList.remove('selected');");
                html.AppendLine("  });");
                html.AppendLine("  selectedNotes = [];");
                html.AppendLine("}");
                html.AppendLine("");
                html.AppendLine("function approveSelectedNotes() {");
                html.AppendLine("  if (selectedNotes.length === 0) {");
                html.AppendLine("    alert('Please select at least one note to approve.');");
                html.AppendLine("    return;");
                html.AppendLine("  }");
                html.AppendLine("");
                html.AppendLine("  // Store selections in hidden field");
                html.AppendLine("  document.getElementById('hdnSelectedNotes').value = JSON.stringify(selectedNotes);");
                html.AppendLine("");
                html.AppendLine("  // Trigger server-side approval");
                html.AppendLine("  document.getElementById('btnServerApproveNotes').click();");
                html.AppendLine("}");
                html.AppendLine("</script>");
            }
            catch (Exception ex)
            {
                return $"<div class=\"error\">Error formatting enhanced notes: {ex.Message}</div>";
            }

            return html.ToString();
        }

        /// <summary>
        /// Formats a section of the enhanced notes
        /// </summary>
        public static string FormatSection(string sectionKey, JToken sectionData)
        {
            var html = new StringBuilder();
            var sectionId = $"section-{sectionKey}";
            var noteId = $"note-{sectionKey}";

            try
            {
                // Extract section information
                string title = sectionKey;
                string style = "bg-secondary text-white"; // Default style

                if (sectionData.Type == JTokenType.Object)
                {
                    var sectionObj = (JObject)sectionData;
                    if (sectionObj["title"] != null)
                    {
                        title = sectionObj["title"].ToString();
                    }

                    if (sectionObj["style"] != null)
                    {
                        style = sectionObj["style"].ToString();
                    }

                    html.AppendLine($"<div id=\"{noteId}\" class=\"section-container\">");
                    html.AppendLine($"  <div class=\"section-select\">");
                    html.AppendLine($"    <input type=\"checkbox\" id=\"chk-{sectionKey}\" class=\"note-checkbox\" onclick=\"toggleNoteSelection('{sectionKey}')\" />");
                    html.AppendLine($"    <label for=\"chk-{sectionKey}\">Include</label>");
                    html.AppendLine($"  </div>");
                    html.AppendLine($"  <div id=\"{sectionId}\" class=\"section\">");
                    html.AppendLine($"    <div class=\"section-header {style}\">");
                    html.AppendLine($"      <h3>{title}</h3>");
                    html.AppendLine($"    </div>");
                    html.AppendLine($"    <div class=\"section-content\">");

                    // Process fields
                    if (sectionObj["fields"] != null && sectionObj["fields"].Type == JTokenType.Object)
                    {
                        html.AppendLine($"      <div class=\"fields-container\">");
                        var fields = (JObject)sectionObj["fields"];
                        foreach (var field in fields.Properties())
                        {
                            string fieldName = FormatFieldName(field.Name);
                            string fieldValue = field.Value.ToString();
                            html.AppendLine($"        <div class=\"field\">");
                            html.AppendLine($"          <span class=\"field-name\">{fieldName}:</span>");
                            html.AppendLine($"          <span class=\"field-value\">{fieldValue}</span>");
                            html.AppendLine($"        </div>");
                        }
                        html.AppendLine($"      </div>");
                    }

                    // Process subsections
                    if (sectionObj["subsections"] != null && sectionObj["subsections"].Type == JTokenType.Object)
                    {
                        html.AppendLine($"      <div class=\"subsections-container\">");
                        var subsections = (JObject)sectionObj["subsections"];
                        foreach (var subsection in subsections.Properties())
                        {
                            string subsectionTitle = subsection.Name;
                            if (subsection.Value["title"] != null)
                            {
                                subsectionTitle = subsection.Value["title"].ToString();
                            }

                            html.AppendLine($"        <div class=\"subsection\">");
                            html.AppendLine($"          <h4 class=\"subsection-title\">{subsectionTitle}</h4>");

                            // Process fields in subsection
                            if (subsection.Value["fields"] != null)
                            {
                                html.AppendLine($"          <div class=\"fields-container\">");
                                var fields = (JObject)subsection.Value["fields"];
                                foreach (var field in fields.Properties())
                                {
                                    string fieldName = FormatFieldName(field.Name);
                                    string fieldValue = field.Value.ToString();
                                    html.AppendLine($"            <div class=\"field\">");
                                    html.AppendLine($"              <span class=\"field-name\">{fieldName}:</span>");
                                    html.AppendLine($"              <span class=\"field-value\">{fieldValue}</span>");
                                    html.AppendLine($"            </div>");
                                }
                                html.AppendLine($"          </div>");
                            }

                            // Process items list
                            if (subsection.Value["items"] != null && subsection.Value["items"].Type == JTokenType.Array)
                            {
                                html.AppendLine($"          <ul class=\"items-list\">");
                                foreach (var item in subsection.Value["items"])
                                {
                                    if (item.Type == JTokenType.String)
                                    {
                                        html.AppendLine($"            <li>{item}</li>");
                                    }
                                    else if (item.Type == JTokenType.Object)
                                    {
                                        // Handle complex items (like diagnoses with codes)
                                        var itemObj = (JObject)item;
                                        if (itemObj["name"] != null)
                                        {
                                            string itemText = itemObj["name"].ToString();
                                            if (itemObj["icd_10_cm_code"] != null)
                                            {
                                                itemText += $" <span class=\"code\">(ICD-10: {itemObj["icd_10_cm_code"]})</span>";
                                            }
                                            else if (itemObj["cpt_code"] != null)
                                            {
                                                itemText += $" <span class=\"code\">(CPT: {itemObj["cpt_code"]})</span>";
                                            }
                                            else if (itemObj["medication_code"] != null)
                                            {
                                                itemText += $" <span class=\"code\">(Code: {itemObj["medication_code"]})</span>";
                                            }

                                            if (itemObj["description"] != null)
                                            {
                                                itemText += $" - {itemObj["description"]}";
                                            }

                                            html.AppendLine($"            <li>{itemText}</li>");
                                        }
                                    }
                                }
                                html.AppendLine($"          </ul>");
                            }

                            html.AppendLine($"        </div>");
                        }
                        html.AppendLine($"      </div>");
                    }

                    // Process conditions
                    if (sectionObj["conditions"] != null && sectionObj["conditions"].Type == JTokenType.Array)
                    {
                        html.AppendLine($"      <div class=\"conditions-container\">");
                        foreach (var condition in sectionObj["conditions"])
                        {
                            if (condition.Type == JTokenType.Object)
                            {
                                var condObj = (JObject)condition;
                                string condTitle = condObj["title"]?.ToString() ?? "Condition";
                                string condDesc = condObj["description"]?.ToString() ?? "";

                                html.AppendLine($"        <div class=\"condition\">");
                                html.AppendLine($"          <h4 class=\"condition-title\">{condTitle}</h4>");
                                html.AppendLine($"          <p class=\"condition-description\">{condDesc}</p>");
                                html.AppendLine($"        </div>");
                            }
                        }
                        html.AppendLine($"      </div>");
                    }

                    // Process procedures
                    if (sectionObj["procedures"] != null && sectionObj["procedures"].Type == JTokenType.Array)
                    {
                        html.AppendLine($"      <div class=\"procedures-container\">");
                        html.AppendLine($"        <ul class=\"procedures-list\">");
                        foreach (var procedure in sectionObj["procedures"])
                        {
                            if (procedure.Type == JTokenType.Object)
                            {
                                var procObj = (JObject)procedure;
                                string procName = procObj["name"]?.ToString() ?? "Procedure";
                                string cptCode = procObj["cpt_code"]?.ToString() ?? "";

                                string procText = procName;
                                if (!string.IsNullOrEmpty(cptCode))
                                {
                                    procText += $" <span class=\"code\">(CPT: {cptCode})</span>";
                                }

                                html.AppendLine($"          <li>{procText}</li>");
                            }
                        }
                        html.AppendLine($"        </ul>");
                        html.AppendLine($"      </div>");
                    }

                    // Process medications
                    if (sectionData.Type == JTokenType.Array && sectionKey.ToLower() == "medications")
                    {
                        html.AppendLine($"      <div class=\"medications-container\">");
                        html.AppendLine($"        <table class=\"medications-table\">");
                        html.AppendLine($"          <thead>");
                        html.AppendLine($"            <tr>");
                        html.AppendLine($"              <th>Medication</th>");
                        html.AppendLine($"              <th>Dosage</th>");
                        html.AppendLine($"              <th>Frequency</th>");
                        html.AppendLine($"              <th>Duration</th>");
                        html.AppendLine($"              <th>Description</th>");
                        html.AppendLine($"            </tr>");
                        html.AppendLine($"          </thead>");
                        html.AppendLine($"          <tbody>");

                        foreach (var med in sectionData)
                        {
                            if (med.Type == JTokenType.Object)
                            {
                                var medObj = (JObject)med;
                                string name = medObj["name"]?.ToString() ?? "";
                                string dosage = medObj["dosage"]?.ToString() ?? "";
                                string frequency = medObj["frequency"]?.ToString() ?? "";
                                string duration = medObj["duration"]?.ToString() ?? "";
                                string description = medObj["description"]?.ToString() ?? "";
                                string code = medObj["medication_code"]?.ToString() ?? "";

                                html.AppendLine($"            <tr>");
                                html.AppendLine($"              <td>{name} {(string.IsNullOrEmpty(code) ? "" : $"<span class=\"code\">({code})</span>")}</td>");
                                html.AppendLine($"              <td>{dosage}</td>");
                                html.AppendLine($"              <td>{frequency}</td>");
                                html.AppendLine($"              <td>{duration}</td>");
                                html.AppendLine($"              <td>{description}</td>");
                                html.AppendLine($"            </tr>");
                            }
                        }

                        html.AppendLine($"          </tbody>");
                        html.AppendLine($"        </table>");
                        html.AppendLine($"      </div>");
                    }

                    html.AppendLine($"    </div>");
                    html.AppendLine($"  </div>");
                    html.AppendLine($"</div>");
                }
                else if (sectionData.Type == JTokenType.Array)
                {
                    // Handle array sections like medications
                    html.AppendLine($"<div id=\"{noteId}\" class=\"section-container\">");
                    html.AppendLine($"  <div class=\"section-select\">");
                    html.AppendLine($"    <input type=\"checkbox\" id=\"chk-{sectionKey}\" class=\"note-checkbox\" onclick=\"toggleNoteSelection('{sectionKey}')\" />");
                    html.AppendLine($"    <label for=\"chk-{sectionKey}\">Include</label>");
                    html.AppendLine($"  </div>");
                    html.AppendLine($"  <div id=\"{sectionId}\" class=\"section\">");
                    html.AppendLine($"    <div class=\"section-header {style}\">");
                    html.AppendLine($"      <h3>{FormatSectionTitle(sectionKey)}</h3>");
                    html.AppendLine($"    </div>");
                    html.AppendLine($"    <div class=\"section-content\">");

                    // For medications, we'll create a table
                    if (sectionKey.ToLower() == "medications")
                    {
                        html.AppendLine($"      <div class=\"medications-container\">");
                        html.AppendLine($"        <table class=\"medications-table\">");
                        html.AppendLine($"          <thead>");
                        html.AppendLine($"            <tr>");
                        html.AppendLine($"              <th>Medication</th>");
                        html.AppendLine($"              <th>Dosage</th>");
                        html.AppendLine($"              <th>Frequency</th>");
                        html.AppendLine($"              <th>Duration</th>");
                        html.AppendLine($"              <th>Description</th>");
                        html.AppendLine($"            </tr>");
                        html.AppendLine($"          </thead>");
                        html.AppendLine($"          <tbody>");

                        foreach (var med in sectionData)
                        {
                            if (med.Type == JTokenType.Object)
                            {
                                var medObj = (JObject)med;
                                string name = medObj["name"]?.ToString() ?? "";
                                string dosage = medObj["dosage"]?.ToString() ?? "";
                                string frequency = medObj["frequency"]?.ToString() ?? "";
                                string duration = medObj["duration"]?.ToString() ?? "";
                                string description = medObj["description"]?.ToString() ?? "";
                                string code = medObj["medication_code"]?.ToString() ?? "";

                                html.AppendLine($"            <tr>");
                                html.AppendLine($"              <td>{name} {(string.IsNullOrEmpty(code) ? "" : $"<span class=\"code\">({code})</span>")}</td>");
                                html.AppendLine($"              <td>{dosage}</td>");
                                html.AppendLine($"              <td>{frequency}</td>");
                                html.AppendLine($"              <td>{duration}</td>");
                                html.AppendLine($"              <td>{description}</td>");
                                html.AppendLine($"            </tr>");
                            }
                        }

                        html.AppendLine($"          </tbody>");
                        html.AppendLine($"        </table>");
                        html.AppendLine($"      </div>");
                    }
                    else
                    {
                        html.AppendLine($"      <div class=\"generic-array\">");
                        html.AppendLine($"        <ul>");
                        foreach (var item in sectionData)
                        {
                            html.AppendLine($"          <li>{item}</li>");
                        }
                        html.AppendLine($"        </ul>");
                        html.AppendLine($"      </div>");
                    }

                    html.AppendLine($"    </div>");
                    html.AppendLine($"  </div>");
                    html.AppendLine($"</div>");
                }
            }
            catch (Exception ex)
            {
                return $"<div class=\"error\">Error formatting section {sectionKey}: {ex.Message}</div>";
            }

            return html.ToString();
        }

        /// <summary>
        /// Formats a field name for display
        /// </summary>
        public static string FormatFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return string.Empty;

            // Replace underscores with spaces
            string result = fieldName.Replace('_', ' ');

            // Handle camelCase by adding spaces before capital letters
            result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

            // Title case the field name
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
        }

        /// <summary>
        /// Formats a section title for display
        /// </summary>
        public static string FormatSectionTitle(string sectionKey)
        {
            if (string.IsNullOrEmpty(sectionKey))
                return string.Empty;

            // Replace underscores with spaces
            string result = sectionKey.Replace('_', ' ');

            // Handle camelCase by adding spaces before capital letters
            result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");

            // Title case the section name
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
        }
    }
}