﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<OpenLawOffice.WebClient.ViewModels.Timing.TimeViewModel>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Edit Time Entry
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    
    <div id="roadmap">
        <div class="zero">Matter: [<%: Html.ActionLink((string)ViewData["Matter"], "Details", "Matters", new { id = ViewData["MatterId"] }, null) %>]</div>
        <div class="one">Task: [<%: Html.ActionLink((string)ViewData["Task"], "Details", "Tasks", new { id = ViewData["TaskId"] }, null) %>]</div>
        <div id="current" class="two">Edit Time Entry<a id="pageInfo" class="btn-question" style="padding-left: 15px;">Help</a></div>
    </div>
    
    <% using (Html.BeginForm())
       {%>
    <%: Html.ValidationSummary(true) %>
    <table class="detail_table">
        <tr>
            <td class="display-label">
                Start<span class="required-field" title="Required Field">*</span>
            </td>
            <td class="display-field">
                <%: Html.TextBoxFor(model => model.Start, new { @Value = Model.Start.ToString("M/d/yyyy h:mm tt") })%>
            </td>
        </tr>
        <tr>
            <td class="display-label">
                Stop<span class="required-field" title="Required Field">*</span>
            </td>
            <td class="display-field">
                <% if (Model.Stop.HasValue)
                   { %>
                    <%: Html.TextBoxFor(model => model.Stop, new { @Value = Model.Stop.Value.ToString("M/d/yyyy h:mm tt") })%>
                <% }
                   else
                   { %>                
                    <%: Html.TextBoxFor(model => model.Stop)%>
                <% } %>
            </td>
        </tr>
        <tr>
            <td class="display-label">
                Billable<span class="required-field" title="Required Field">*</span>
            </td>
            <td class="display-field">
                <%: Html.CheckBoxFor(model => model.Billable)%>
                Check if time is billable.
            </td>
        </tr>
        <tr>
            <td class="display-label">
                Worker<span class="required-field" title="Required Field">*</span>
            </td>
            <td class="display-field">
            
                <%: Html.DropDownListFor(model => model.Worker.Id,
                        new SelectList((IList)ViewData["EmployeeContactList"], "Id", "DisplayName"),
                        new { @size = 5 })%>

                </script>
            </td>
        </tr>
        <tr>
            <td class="display-label">
                Details:
            </td>
            <td class="display-field">
                <%: Html.TextAreaFor(model => model.Details, new { style = "width: 500px; height: 100px;" })%>
            </td>
        </tr>
    </table>
    <p>
        <input type="submit" value="Save" />
    </p>
    <% } %>
    <div id="pageInfoDialog" title="Help">
        <p>
        <span style="font-weight: bold; text-decoration: underline;">Info:</span>
        Fill in the information on this page to modify the time entry.  Required fields are indicated with an
        <span style="color: #ee0000;font-size: 12px;cursor:help;" title="Required Field">*</span><br /><br />
        <span style="font-weight: bold; text-decoration: underline;">Usage:</span>
        Fields marked with an <span style="color: #ee0000;font-size: 12px;cursor:help;" title="Required Field">*</span> are required.
        </p>
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MenuContent" runat="server">
    <li>Actions</li>
    <ul style="list-style: none outside none; padding-left: 1em;">
        <li>
            <%: Html.ActionLink("Details", "Details", new { id = Model.Id })%></li>
    </ul>
    <% if (ViewData["TaskId"] != null)
        { %>
    <li>
        <%: Html.ActionLink("Task ", "Details", "Tasks", new { id = ViewData["TaskId"] }, null)%></li>
    <li>
        <%: Html.ActionLink("Times for Task", "Time", "Tasks", new { id = ViewData["TaskId"] }, null)%></li>
    <% } %>
</asp:Content>