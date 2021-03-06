﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindHorizon.Common;
using MindHorizon.Data.Contracts;
using MindHorizon.ViewModels.Dashboard;
using MindHorizon.ViewModels.DynamicAccess;

namespace MindHorizon.Areas.Admin.Controllers
{
    [DisplayName("مدیریت داشبورد")]
    public class DashboardController : BaseController
    {
        private readonly IUnitOfWork _uw;
        public DashboardController(IUnitOfWork uw)
        {
            _uw = uw;
        }

        [HttpGet, DisplayName("نمایش داشبورد مدیریت")]
        [Authorize(Policy = ConstantPolicies.DynamicPermission)]
        public IActionResult Index()
        {
            ViewBag.Posts = _uw.PostRepository.CountPosts();
            ViewBag.FuturePublishedPosts = _uw.PostRepository.CountFuturePublishedPosts();
            ViewBag.PostsPublished = _uw.PostRepository.CountPostsPublishedOrDraft(true);
            ViewBag.DraftPosts = _uw.PostRepository.CountPostsPublishedOrDraft(false);

            var month = StringExtensions.GetMonth();
            int numberOfVisit;
            var year = DateTimeExtensions.ConvertMiladiToShamsi(DateTime.Now, "yyyy");
            DateTime StartDateTimeMiladi;
            DateTime EndDateTimeMiladi;
            var numberOfVisitList = new List<NumberOfVisitChartViewModel>();

            for (int i = 0; i < month.Length; i++)
            {
                StartDateTimeMiladi = DateTimeExtensions.ConvertShamsiToMiladi($"{year}/{i + 1}/01");
                if (i < 11)
                    EndDateTimeMiladi = DateTimeExtensions.ConvertShamsiToMiladi($"{year}/{i + 2}/01");
                else
                    EndDateTimeMiladi = DateTimeExtensions.ConvertShamsiToMiladi($"{year}/01/01");

                numberOfVisit = _uw._Context.Post.Where(n => n.PublishDateTime < EndDateTimeMiladi && StartDateTimeMiladi <= n.PublishDateTime).Include(v => v.Visits).Select(k => k.Visits.Sum(v => v.NumberOfVisit)).AsEnumerable().Sum();
                numberOfVisitList.Add(new NumberOfVisitChartViewModel { Name = month[i], Value = numberOfVisit });
            }

            ViewBag.NumberOfVisitChart = numberOfVisitList;
            return View();
        }
    }
}