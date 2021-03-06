﻿// -----------------------------------------------------------------------
// <copyright file="BillingController.cs" company="Nodine Legal, LLC">
// Licensed to Nodine Legal, LLC under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  Nodine Legal, LLC licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
// </copyright>
// -----------------------------------------------------------------------

namespace OpenLawOffice.WebClient.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;

    [HandleError(View = "Errors/Index", Order = 10)]
    public class BillingController : BaseController
    {
        public class RateListItem
        {
            public string Title { get; set; }
        }

        [Authorize(Roles = "Login, User")]
        public ActionResult Index()
        {
            ViewModels.Billing.BillingViewModel viewModel = new ViewModels.Billing.BillingViewModel();
            ViewModels.Billing.BillingViewModel.Item item;
            ViewModels.Billing.BillingViewModel.GroupItem groupItem;

            Data.Billing.Invoice.ListBillableMatters().ForEach(matter =>
            {
                item = new ViewModels.Billing.BillingViewModel.Item();
                item.Matter = Mapper.Map<ViewModels.Matters.MatterViewModel>(matter);
                if (matter.BillTo != null && matter.BillTo.Id.HasValue)
                    item.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(matter.BillTo.Id.Value));
                item.Expenses = Data.Billing.Expense.SumUnbilledExpensesForMatter(matter.Id.Value);
                item.Fees = Data.Billing.Fee.SumUnbilledFeesForMatter(matter.Id.Value);
                item.Time = Data.Timing.Time.SumUnbilledAndBillableTimeForMatter(matter.Id.Value);

                if ((item.Expenses + item.Fees + (decimal)item.Time.TotalHours) > 0)
                    viewModel.Items.Add(item);
            });

            Data.Billing.Invoice.ListBillableBillingGroups().ForEach(group =>
            {
                groupItem = new ViewModels.Billing.BillingViewModel.GroupItem();
                groupItem.BillingGroup = Mapper.Map<ViewModels.Billing.BillingGroupViewModel>(group);
                groupItem.BillingGroup.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(group.BillTo.Id.Value));
                groupItem.Expenses = Data.Billing.BillingGroup.SumBillableExpensesForGroup(group.Id.Value);
                groupItem.Fees = Data.Billing.BillingGroup.SumBillableFeesForGroup(group.Id.Value);
                groupItem.Time = Data.Timing.Time.SumUnbilledAndBillableTimeForBillingGroup(group.Id.Value);
                viewModel.GroupItems.Add(groupItem);
            });

            Data.Billing.Invoice.GetMostRecentInvoices(10).ForEach(invoice =>
            {
                ViewModels.Billing.InvoiceViewModel vm = Mapper.Map<ViewModels.Billing.InvoiceViewModel>(invoice);

                if (invoice.Matter != null && invoice.Matter.Id.HasValue)
                    vm.Matter = Mapper.Map<ViewModels.Matters.MatterViewModel>(Data.Matters.Matter.Get(invoice.Matter.Id.Value));
                else
                    vm.Matter = null;

                if (invoice.BillingGroup != null && invoice.BillingGroup.Id.HasValue)
                    vm.BillingGroup = Mapper.Map<ViewModels.Billing.BillingGroupViewModel>(Data.Billing.BillingGroup.Get(invoice.BillingGroup.Id.Value));
                else
                    vm.BillingGroup = null;

                vm.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(invoice.BillTo.Id.Value));

                viewModel.RecentInvoices.Add(vm);
            });

            return View(viewModel);
        }

        public ViewModels.Billing.InvoiceViewModel BuildSingleMatterViewModel(Guid id, DateTime? startDate = null, DateTime? stopDate = null, string rateFrom = null)
        {
            Common.Models.Billing.BillingRate billingRate = null;
            ViewModels.Billing.InvoiceViewModel viewModel = new ViewModels.Billing.InvoiceViewModel();
            Common.Models.Billing.Invoice previousInvoice = null;
            Common.Models.Matters.Matter matter;

            matter = Data.Matters.Matter.Get(id);

            previousInvoice = Data.Billing.Invoice.GetMostRecentInvoiceForContact(matter.BillTo.Id.Value);

            // Set default billing rate
            if (matter.DefaultBillingRate != null && matter.DefaultBillingRate.Id.HasValue)
                billingRate = Data.Billing.BillingRate.Get(matter.DefaultBillingRate.Id.Value);

            viewModel.Id = Guid.NewGuid();
            viewModel.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(matter.BillTo.Id.Value));
            viewModel.Date = DateTime.Now;
            viewModel.Due = DateTime.Now.AddDays(30);
            viewModel.Matter = Mapper.Map<ViewModels.Matters.MatterViewModel>(matter);

            if (previousInvoice == null)
            {
                viewModel.BillTo_NameLine1 = viewModel.BillTo.DisplayName;
                if (string.IsNullOrEmpty(viewModel.BillTo.Address1AddressPostOfficeBox))
                    viewModel.BillTo_AddressLine1 = viewModel.BillTo.Address1AddressStreet;
                else
                    viewModel.BillTo_AddressLine1 = "P.O. Box " + viewModel.BillTo.Address1AddressPostOfficeBox;
                viewModel.BillTo_City = viewModel.BillTo.Address1AddressCity;
                viewModel.BillTo_State = viewModel.BillTo.Address1AddressStateOrProvince;
                viewModel.BillTo_Zip = viewModel.BillTo.Address1AddressPostalCode;
            }
            else
            {
                viewModel.BillTo_NameLine1 = previousInvoice.BillTo_NameLine1;
                viewModel.BillTo_NameLine2 = previousInvoice.BillTo_NameLine2;
                viewModel.BillTo_AddressLine1 = previousInvoice.BillTo_AddressLine1;
                viewModel.BillTo_AddressLine2 = previousInvoice.BillTo_AddressLine2;
                viewModel.BillTo_City = previousInvoice.BillTo_City;
                viewModel.BillTo_State = previousInvoice.BillTo_State;
                viewModel.BillTo_Zip = previousInvoice.BillTo_Zip;
            }

            Data.Timing.Time.ListUnbilledAndBillableTimeForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
            {
                ViewModels.Billing.InvoiceTimeViewModel vm = new ViewModels.Billing.InvoiceTimeViewModel()
                {
                    Invoice = viewModel,
                    Time = Mapper.Map<ViewModels.Timing.TimeViewModel>(x),
                    Details = x.Details
                };
                if (x.Stop.HasValue)
                    vm.Duration = x.Stop.Value - x.Start;
                else
                    vm.Duration = new TimeSpan(0);

                if (string.IsNullOrEmpty(rateFrom))
                { // Not specified in URL
                    if (matter.OverrideMatterRateWithEmployeeRate)
                    {
                        Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
                        if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
                            billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
                    }
                }
                else
                { // Overridden by current user in URL
                    if (rateFrom == "employee")
                    {
                        Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
                        if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
                            billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
                    }
                }

                if (billingRate != null)
                    vm.PricePerHour = billingRate.PricePerUnit;
                viewModel.Times.Add(vm);
            });

            Data.Billing.Expense.ListUnbilledExpensesForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
            {
                viewModel.Expenses.Add(new ViewModels.Billing.InvoiceExpenseViewModel()
                {
                    Invoice = viewModel,
                    Expense = Mapper.Map<ViewModels.Billing.ExpenseViewModel>(x),
                    Details = x.Details,
                    Amount = x.Amount
                });
            });

            Data.Billing.Fee.ListUnbilledFeesForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
            {
                viewModel.Fees.Add(new ViewModels.Billing.InvoiceFeeViewModel()
                {
                    Invoice = viewModel,
                    Fee = Mapper.Map<ViewModels.Billing.FeeViewModel>(x),
                    Details = x.Details,
                    Amount = x.Amount
                });
            });

            ViewData["MatterTitle"] = matter.Title;
            ViewData["CaseNumber"] = matter.CaseNumber;
            ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
            ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
            ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
            ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
            ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
            ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
            ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;

            return viewModel;
        }

        [Authorize(Roles = "Login, User")]
        public ActionResult SingleMatterBill(Guid id)
        {
            Common.Models.Matters.Matter matter;
            DateTime? start = null, stop = null;
            ViewModels.Billing.InvoiceViewModel viewModel = new ViewModels.Billing.InvoiceViewModel();

            if (!string.IsNullOrEmpty(Request["StartDate"]))
                start = DateTime.Parse(Request["StartDate"]);
            if (!string.IsNullOrEmpty(Request["StopDate"]))
                stop = DateTime.Parse(Request["StopDate"]);

            matter = Data.Matters.Matter.Get(id);
            viewModel = BuildSingleMatterViewModel(id, start, stop, Request["rateFrom"]);

            ViewData["MatterTitle"] = matter.Title;
            ViewData["CaseNumber"] = matter.CaseNumber;
            ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
            ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
            ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
            ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
            ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
            ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
            ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;

            return View(viewModel);
        }

        [Authorize(Roles = "Login, User")]
        [HttpPost]
        public ActionResult SingleMatterBill(Guid id, ViewModels.Billing.InvoiceViewModel viewModel)
        {
            // Create Invoice
            // Loop Expenses
            // Loop Fees
            // Loop Times
            // Redirect to invoice viewing
            Common.Models.Account.Users currentUser;
            Common.Models.Matters.Matter matter;
            DateTime? start = null, stop = null;
            List<Common.Models.Billing.InvoiceExpense> invoiceExpenseList;
            List<Common.Models.Billing.InvoiceFee> invoiceFeeList;
            List<Common.Models.Billing.InvoiceTime> invoiceTimeList;

            if (!string.IsNullOrEmpty(Request["StartDate"]))
                start = DateTime.Parse(Request["StartDate"]);
            if (!string.IsNullOrEmpty(Request["StopDate"]))
                stop = DateTime.Parse(Request["StopDate"]);

            currentUser = Data.Account.Users.Get(User.Identity.Name);
            Common.Models.Billing.Invoice invoice = Mapper.Map<Common.Models.Billing.Invoice>(viewModel);
            ViewModels.Billing.InvoiceViewModel builtInvoice = BuildSingleMatterViewModel(id, start, stop, Request["rateFrom"]);
            ViewModels.Billing.InvoiceExpenseViewModel ievm;
            ViewModels.Billing.InvoiceFeeViewModel ifvm;
            ViewModels.Billing.InvoiceTimeViewModel itvm;

            // Validation
            for (int i = 0; i < viewModel.Expenses.Count; i++)
            {
                ievm = builtInvoice.Expenses.Single(x => x.Expense.Id.Value == viewModel.Expenses[i].Expense.Id.Value);
                viewModel.Expenses[i].Expense = ievm.Expense;
                if (string.IsNullOrEmpty(viewModel.Expenses[i].Details))
                    ModelState.AddModelError(string.Format("Expenses[{0}].Details", i), "Required");
            };
            for (int i = 0; i < viewModel.Fees.Count; i++)
            {
                ifvm = builtInvoice.Fees.Single(x => x.Fee.Id.Value == viewModel.Fees[i].Fee.Id.Value);
                viewModel.Fees[i].Fee = ifvm.Fee;
                if (string.IsNullOrEmpty(viewModel.Fees[i].Details))
                    ModelState.AddModelError(string.Format("Fees[{0}].Details", i), "Required");
            };
            for (int i = 0; i < viewModel.Times.Count; i++)
            {
                itvm = builtInvoice.Times.Single(x => x.Time.Id.Value == viewModel.Times[i].Time.Id);
                viewModel.Times[i].Time = itvm.Time;
                if (string.IsNullOrEmpty(viewModel.Times[i].Details))
                    ModelState.AddModelError(string.Format("Times[{0}].Details", i), "Required");
            };

            if (!ModelState.IsValid)
            {
                // Errors - do nothing, but tell user and show again for fixing
                matter = Data.Matters.Matter.Get(id);
                ViewData["MatterTitle"] = matter.Title;
                ViewData["CaseNumber"] = matter.CaseNumber;
                ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
                ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
                ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
                ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
                ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
                ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
                ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;
                return View(viewModel);
            }

            invoiceExpenseList = new List<Common.Models.Billing.InvoiceExpense>();
            invoiceFeeList = new List<Common.Models.Billing.InvoiceFee>();
            invoiceTimeList = new List<Common.Models.Billing.InvoiceTime>();

            viewModel.Expenses.ForEach(vm =>
            {
                invoiceExpenseList.Add(new Common.Models.Billing.InvoiceExpense()
                {
                    Invoice = invoice,
                    Expense = new Common.Models.Billing.Expense()
                    {
                        Id = vm.Expense.Id
                    },
                    Amount = vm.Amount,
                    Details = vm.Details
                });
            });

            viewModel.Fees.ForEach(vm =>
            {
                invoiceFeeList.Add(new Common.Models.Billing.InvoiceFee()
                {
                    Invoice = invoice,
                    Fee = new Common.Models.Billing.Fee()
                    {
                        Id = vm.Fee.Id
                    },
                    Amount = vm.Amount,
                    Details = vm.Details
                });
            });

            viewModel.Times.ForEach(vm =>
            {
                invoiceTimeList.Add(new Common.Models.Billing.InvoiceTime()
                {
                    Invoice = invoice,
                    Time = new Common.Models.Timing.Time()
                    {
                        Id = vm.Time.Id
                    },
                    Duration = vm.Duration,
                    PricePerHour = vm.PricePerHour,
                    Details = vm.Details
                });
            });

            // Clear id from invoice as the ID is the ID of the matter
            invoice.Id = null;

            invoice = Data.Billing.Billing.SingleMatterBill(
                invoice,
                invoiceExpenseList,
                invoiceFeeList,
                invoiceTimeList,
                currentUser,
                null, true);

            return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
        }

        public ViewModels.Billing.GroupInvoiceViewModel BuildGroupInvoiceViewModel(int id, DateTime? startDate = null, DateTime? stopDate = null, string rateFrom = null)
        {
            Common.Models.Billing.BillingRate billingRate = null;
            Common.Models.Billing.BillingGroup billingGroup;
            ViewModels.Billing.GroupInvoiceViewModel viewModel = new ViewModels.Billing.GroupInvoiceViewModel();
            Common.Models.Billing.Invoice previousInvoice = null;
            List<Common.Models.Matters.Matter> mattersList;

            if (!string.IsNullOrEmpty(Request["StartDate"]))
                startDate = DateTime.Parse(Request["StartDate"]);
            if (!string.IsNullOrEmpty(Request["StopDate"]))
                stopDate = DateTime.Parse(Request["StopDate"]);

            billingGroup = Data.Billing.BillingGroup.Get(id);
            previousInvoice = Data.Billing.Invoice.GetMostRecentInvoiceForContact(billingGroup.BillTo.Id.Value);

            billingGroup.NextRun = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1);

            viewModel.Id = Guid.NewGuid();
            viewModel.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(billingGroup.BillTo.Id.Value));
            viewModel.Date = DateTime.Now;
            viewModel.Due = DateTime.Now.AddDays(30);
            viewModel.BillingGroup = Mapper.Map<ViewModels.Billing.BillingGroupViewModel>(billingGroup);

            if (previousInvoice == null)
            {
                viewModel.BillTo_NameLine1 = viewModel.BillTo.DisplayName;
                if (string.IsNullOrEmpty(viewModel.BillTo.Address1AddressPostOfficeBox))
                    viewModel.BillTo_AddressLine1 = viewModel.BillTo.Address1AddressStreet;
                else
                    viewModel.BillTo_AddressLine1 = "P.O. Box " + viewModel.BillTo.Address1AddressPostOfficeBox;
                viewModel.BillTo_City = viewModel.BillTo.Address1AddressCity;
                viewModel.BillTo_State = viewModel.BillTo.Address1AddressStateOrProvince;
                viewModel.BillTo_Zip = viewModel.BillTo.Address1AddressPostalCode;
            }
            else
            {
                viewModel.BillTo_NameLine1 = previousInvoice.BillTo_NameLine1;
                viewModel.BillTo_NameLine2 = previousInvoice.BillTo_NameLine2;
                viewModel.BillTo_AddressLine1 = previousInvoice.BillTo_AddressLine1;
                viewModel.BillTo_AddressLine2 = previousInvoice.BillTo_AddressLine2;
                viewModel.BillTo_City = previousInvoice.BillTo_City;
                viewModel.BillTo_State = previousInvoice.BillTo_State;
                viewModel.BillTo_Zip = previousInvoice.BillTo_Zip;
            }

            // Get list of matters for the group
            mattersList = Data.Billing.BillingGroup.ListMattersForGroup(billingGroup.Id.Value);

            for (int i = 0; i < mattersList.Count; i++)
            {
                ViewModels.Billing.GroupInvoiceItemViewModel giivm = new ViewModels.Billing.GroupInvoiceItemViewModel();
                Common.Models.Matters.Matter matter = mattersList[i];

                giivm.Matter = Mapper.Map<ViewModels.Matters.MatterViewModel>(matter);

                Data.Timing.Time.ListUnbilledAndBillableTimeForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
                {
                    ViewModels.Billing.InvoiceTimeViewModel vm = new ViewModels.Billing.InvoiceTimeViewModel()
                    {
                        //Invoice = viewModel,
                        Time = Mapper.Map<ViewModels.Timing.TimeViewModel>(x),
                        Details = x.Details
                    };
                    if (x.Stop.HasValue)
                        vm.Duration = x.Stop.Value - x.Start;
                    else
                        vm.Duration = new TimeSpan(0);

                    if (string.IsNullOrEmpty(Request["RateFrom"]))
                    { // Not specified in URL
                        if (matter.OverrideMatterRateWithEmployeeRate)
                        {
                            Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
                            if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
                                billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
                        }
                    }
                    else
                    { // Overridden by current user in URL
                        if (Request["RateFrom"] == "Employee")
                        {
                            Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
                            if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
                                billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
                        }
                    }

                    if (billingRate != null)
                        vm.PricePerHour = billingRate.PricePerUnit;

                    giivm.Times.Add(vm);
                });

                Data.Billing.Expense.ListUnbilledExpensesForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
                {
                    giivm.Expenses.Add(new ViewModels.Billing.InvoiceExpenseViewModel()
                    {
                        //Invoice = viewModel,
                        Expense = Mapper.Map<ViewModels.Billing.ExpenseViewModel>(x),
                        Details = x.Details,
                        Amount = x.Amount
                    });
                });

                Data.Billing.Fee.ListUnbilledFeesForMatter(matter.Id.Value, startDate, stopDate).ForEach(x =>
                {
                    giivm.Fees.Add(new ViewModels.Billing.InvoiceFeeViewModel()
                    {
                        //Invoice = viewModel,
                        Fee = Mapper.Map<ViewModels.Billing.FeeViewModel>(x),
                        Details = x.Details,
                        Amount = x.Amount
                    });
                });

                if ((giivm.Times.Count > 0) ||
                    (giivm.Expenses.Count > 0) ||
                    (giivm.Fees.Count > 0))
                    viewModel.Matters.Add(giivm);
            }

            ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
            ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
            ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
            ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
            ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
            ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
            ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;

            return viewModel;
        }

        [Authorize(Roles = "Login, User")]
        public ActionResult SingleGroupBill(int id)
        {
            DateTime? start = null, stop = null;
            Common.Models.Billing.BillingGroup billingGroup;
            ViewModels.Billing.GroupInvoiceViewModel viewModel = new ViewModels.Billing.GroupInvoiceViewModel();

            if (!string.IsNullOrEmpty(Request["StartDate"]))
                start = DateTime.Parse(Request["StartDate"]);
            if (!string.IsNullOrEmpty(Request["StopDate"]))
                stop = DateTime.Parse(Request["StopDate"]);

            billingGroup = Data.Billing.BillingGroup.Get(id);
            viewModel = BuildGroupInvoiceViewModel(id, start, stop, Request["rateFrom"]);

            //Common.Models.Billing.BillingRate billingRate = null;
            //Common.Models.Billing.Invoice previousInvoice = null;
            //List<Common.Models.Matters.Matter> mattersList;

            //previousInvoice = Data.Billing.Invoice.GetMostRecentInvoiceForContact(billingGroup.BillTo.Id.Value);

            //billingGroup.NextRun = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1);

            //viewModel.Id = Guid.NewGuid();
            //viewModel.BillTo = Mapper.Map<ViewModels.Contacts.ContactViewModel>(Data.Contacts.Contact.Get(billingGroup.BillTo.Id.Value));
            //viewModel.Date = DateTime.Now;
            //viewModel.Due = DateTime.Now.AddDays(30);
            //viewModel.BillingGroup = Mapper.Map<ViewModels.Billing.BillingGroupViewModel>(billingGroup);

            //if (previousInvoice == null)
            //{
            //    viewModel.BillTo_NameLine1 = viewModel.BillTo.DisplayName;
            //    if (string.IsNullOrEmpty(viewModel.BillTo.Address1AddressPostOfficeBox))
            //        viewModel.BillTo_AddressLine1 = viewModel.BillTo.Address1AddressStreet;
            //    else
            //        viewModel.BillTo_AddressLine1 = "P.O. Box " + viewModel.BillTo.Address1AddressPostOfficeBox;
            //    viewModel.BillTo_City = viewModel.BillTo.Address1AddressCity;
            //    viewModel.BillTo_State = viewModel.BillTo.Address1AddressStateOrProvince;
            //    viewModel.BillTo_Zip = viewModel.BillTo.Address1AddressPostalCode;
            //}
            //else
            //{
            //    viewModel.BillTo_NameLine1 = previousInvoice.BillTo_NameLine1;
            //    viewModel.BillTo_NameLine2 = previousInvoice.BillTo_NameLine2;
            //    viewModel.BillTo_AddressLine1 = previousInvoice.BillTo_AddressLine1;
            //    viewModel.BillTo_AddressLine2 = previousInvoice.BillTo_AddressLine2;
            //    viewModel.BillTo_City = previousInvoice.BillTo_City;
            //    viewModel.BillTo_State = previousInvoice.BillTo_State;
            //    viewModel.BillTo_Zip = previousInvoice.BillTo_Zip;
            //}

            //mattersList = Data.Billing.BillingGroup.ListMattersForGroup(billingGroup.Id.Value);

            //if (!string.IsNullOrEmpty(Request["StartDate"]))
            //    start = DateTime.Parse(Request["StartDate"]);
            //if (!string.IsNullOrEmpty(Request["StopDate"]))
            //    stop = DateTime.Parse(Request["StopDate"]);

            //for (int i=0; i<mattersList.Count; i++)
            //{
            //    ViewModels.Billing.GroupInvoiceItemViewModel giivm = new ViewModels.Billing.GroupInvoiceItemViewModel();
            //    Common.Models.Matters.Matter matter = mattersList[i];

            //    giivm.Matter = Mapper.Map<ViewModels.Matters.MatterViewModel>(matter);

            //    Data.Timing.Time.ListUnbilledAndBillableTimeForMatter(matter.Id.Value, start, stop).ForEach(x =>
            //    {
            //        ViewModels.Billing.InvoiceTimeViewModel vm = new ViewModels.Billing.InvoiceTimeViewModel()
            //        {
            //            //Invoice = viewModel,
            //            Time = Mapper.Map<ViewModels.Timing.TimeViewModel>(x),
            //            Details = x.Details
            //        };
            //        if (x.Stop.HasValue)
            //            vm.Duration = x.Stop.Value - x.Start;
            //        else
            //            vm.Duration = new TimeSpan(0);

            //        if (string.IsNullOrEmpty(Request["RateFrom"]))
            //        { // Not specified in URL
            //            if (matter.OverrideMatterRateWithEmployeeRate)
            //            {
            //                Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
            //                if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
            //                    billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
            //            }
            //        }
            //        else
            //        { // Overridden by current user in URL
            //            if (Request["RateFrom"] == "Employee")
            //            {
            //                Common.Models.Contacts.Contact contact = Data.Contacts.Contact.Get(x.Worker.Id.Value);
            //                if (contact.BillingRate != null && contact.BillingRate.Id.HasValue)
            //                    billingRate = Data.Billing.BillingRate.Get(contact.BillingRate.Id.Value);
            //            }
            //        }

            //        if (billingRate != null)
            //            vm.PricePerHour = billingRate.PricePerUnit;

            //        giivm.Times.Add(vm);
            //    });

            //    Data.Billing.Expense.ListUnbilledExpensesForMatter(matter.Id.Value, start, stop).ForEach(x =>
            //    {
            //        giivm.Expenses.Add(new ViewModels.Billing.InvoiceExpenseViewModel()
            //        {
            //            //Invoice = viewModel,
            //            Expense = Mapper.Map<ViewModels.Billing.ExpenseViewModel>(x),
            //            Details = x.Details,
            //            Amount = x.Amount
            //        });
            //    });

            //    Data.Billing.Fee.ListUnbilledFeesForMatter(matter.Id.Value, start, stop).ForEach(x =>
            //    {
            //        giivm.Fees.Add(new ViewModels.Billing.InvoiceFeeViewModel()
            //        {
            //            //Invoice = viewModel,
            //            Fee = Mapper.Map<ViewModels.Billing.FeeViewModel>(x),
            //            Details = x.Details,
            //            Amount = x.Amount
            //        });
            //    });

            //    if ((giivm.Times.Count > 0) ||
            //        (giivm.Expenses.Count > 0) ||
            //        (giivm.Fees.Count > 0))
            //        viewModel.Matters.Add(giivm);
            //}

            ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
            ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
            ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
            ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
            ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
            ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
            ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;

            return View(viewModel);
        }

        [Authorize(Roles = "Login, User")]
        [HttpPost]
        public ActionResult SingleGroupBill(int id, ViewModels.Billing.GroupInvoiceViewModel viewModel)
        {
            // Create Invoice
            // Loop Matters
                // Loop Expenses
                // Loop Fees
                // Loop Times
            // Redirect to invoice viewing
            DateTime? start = null, stop = null;
            Common.Models.Account.Users currentUser;
            Common.Models.Billing.BillingGroup billingGroup;
            ViewModels.Billing.GroupInvoiceViewModel builtGroupInvoice;
            ViewModels.Billing.InvoiceExpenseViewModel ievm;
            ViewModels.Billing.InvoiceFeeViewModel ifvm;
            ViewModels.Billing.InvoiceTimeViewModel itvm;
            List<Common.Models.Billing.InvoiceExpense> invoiceExpenseList;
            List<Common.Models.Billing.InvoiceFee> invoiceFeeList;
            List<Common.Models.Billing.InvoiceTime> invoiceTimeList;

            if (!string.IsNullOrEmpty(Request["StartDate"]))
                start = DateTime.Parse(Request["StartDate"]);
            if (!string.IsNullOrEmpty(Request["StopDate"]))
                stop = DateTime.Parse(Request["StopDate"]);

            currentUser = Data.Account.Users.Get(User.Identity.Name);
            billingGroup = Data.Billing.BillingGroup.Get(id);
            billingGroup.Amount = viewModel.BillingGroup.Amount;
            billingGroup.NextRun = viewModel.BillingGroup.NextRun;

            builtGroupInvoice = BuildGroupInvoiceViewModel(id, start, stop, Request["rateFrom"]);

            // Validation

            // Loop for each matter
            for (int j = 0; j < viewModel.Matters.Count; j++)
            {
                ViewModels.Billing.GroupInvoiceItemViewModel giivm =
                    builtGroupInvoice.Matters.Single(x => x.Matter.Id.Value == viewModel.Matters[j].Matter.Id.Value);

                for (int i = 0; i < viewModel.Matters[j].Expenses.Count; i++)
                {
                    ievm = giivm.Expenses.Single(x => x.Expense.Id.Value == viewModel.Matters[j].Expenses[i].Expense.Id.Value);
                    viewModel.Matters[j].Expenses[i].Expense = ievm.Expense;
                    if (string.IsNullOrEmpty(viewModel.Matters[j].Expenses[i].Details))
                        ModelState.AddModelError(string.Format("Matters[{0}].Expenses[{1}].Details", j, i), "Required");
                };
                for (int i = 0; i < viewModel.Matters[j].Fees.Count; i++)
                {
                    ifvm = giivm.Fees.Single(x => x.Fee.Id.Value == viewModel.Matters[j].Fees[i].Fee.Id.Value);
                    viewModel.Matters[j].Fees[i].Fee = ifvm.Fee;
                    if (string.IsNullOrEmpty(viewModel.Matters[j].Fees[i].Details))
                        ModelState.AddModelError(string.Format("Matters[{0}].Fees[{1}].Details", j, i), "Required");
                };
                for (int i = 0; i < viewModel.Matters[j].Times.Count; i++)
                {
                    itvm = giivm.Times.Single(x => x.Time.Id.Value == viewModel.Matters[j].Times[i].Time.Id);
                    viewModel.Matters[j].Times[i].Time = itvm.Time;
                    if (string.IsNullOrEmpty(viewModel.Matters[j].Times[i].Details))
                        ModelState.AddModelError(string.Format("Matters[{0}].Times[{1}].Details", j, i), "Required");
                };
            }

            ModelState["id"].Errors.Clear();
            if (!ModelState.IsValid)
            {
                // Errors - do nothing, but tell user and show again for fixing
                ViewData["FirmName"] = Common.Settings.Manager.Instance.System.BillingFirmName;
                ViewData["FirmAddress"] = Common.Settings.Manager.Instance.System.BillingFirmAddress;
                ViewData["FirmCity"] = Common.Settings.Manager.Instance.System.BillingFirmCity;
                ViewData["FirmState"] = Common.Settings.Manager.Instance.System.BillingFirmState;
                ViewData["FirmZip"] = Common.Settings.Manager.Instance.System.BillingFirmZip;
                ViewData["FirmPhone"] = Common.Settings.Manager.Instance.System.BillingFirmPhone;
                ViewData["FirmWeb"] = Common.Settings.Manager.Instance.System.BillingFirmWeb;
                return View(viewModel);
            }

            Common.Models.Billing.Invoice invoice = new Common.Models.Billing.Invoice()
            {
                Id = viewModel.Id,
                BillTo = Mapper.Map<Common.Models.Contacts.Contact>(viewModel.BillTo),
                Date = viewModel.Date,
                Due = viewModel.Due,
                Subtotal = viewModel.Subtotal,
                TaxAmount = viewModel.TaxAmount,
                Total = viewModel.Total,
                ExternalInvoiceId = viewModel.ExternalInvoiceId,
                BillTo_NameLine1 = viewModel.BillTo_NameLine1,
                BillTo_NameLine2 = viewModel.BillTo_NameLine2,
                BillTo_AddressLine1 = viewModel.BillTo_AddressLine1,
                BillTo_AddressLine2 = viewModel.BillTo_AddressLine2,
                BillTo_City = viewModel.BillTo_City,
                BillTo_State = viewModel.BillTo_State,
                BillTo_Zip = viewModel.BillTo_Zip,
                BillingGroup = new Common.Models.Billing.BillingGroup() { Id = id }
            };

            invoiceExpenseList = new List<Common.Models.Billing.InvoiceExpense>();
            invoiceFeeList = new List<Common.Models.Billing.InvoiceFee>();
            invoiceTimeList = new List<Common.Models.Billing.InvoiceTime>();

            viewModel.Matters.ForEach(matterVM =>
            {
                matterVM.Expenses.ForEach(vm =>
                {
                    invoiceExpenseList.Add(new Common.Models.Billing.InvoiceExpense()
                    {
                        Invoice = invoice,
                        Expense = new Common.Models.Billing.Expense()
                        {
                            Id = vm.Expense.Id
                        },
                        Amount = vm.Amount,
                        Details = vm.Details
                    });
                });

                matterVM.Fees.ForEach(vm =>
                {
                    invoiceFeeList.Add(new Common.Models.Billing.InvoiceFee()
                    {
                        Invoice = invoice,
                        Fee = new Common.Models.Billing.Fee()
                        {
                            Id = vm.Fee.Id
                        },
                        Amount = vm.Amount,
                        Details = vm.Details
                    });
                });

                matterVM.Times.ForEach(vm =>
                {
                    invoiceTimeList.Add(new Common.Models.Billing.InvoiceTime()
                    {
                        Invoice = invoice,
                        Time = new Common.Models.Timing.Time()
                        {
                            Id = vm.Time.Id
                        },
                        Duration = vm.Duration,
                        PricePerHour = vm.PricePerHour,
                        Details = vm.Details
                    });
                });
            });

            invoice = Data.Billing.Billing.SingleGroupBill(
                billingGroup,
                invoice,
                invoiceExpenseList,
                invoiceFeeList,
                invoiceTimeList,
                currentUser,
                null, true);

            return RedirectToAction("GroupDetails", "Invoices", new { id = invoice.Id });
        }
    }
}