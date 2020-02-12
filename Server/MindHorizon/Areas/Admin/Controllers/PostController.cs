﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindHorizon.Common;
using MindHorizon.Common.Attributes;
using MindHorizon.Data.Contracts;
using MindHorizon.Entities;
using MindHorizon.ViewModels.Post;

namespace MindHorizon.Areas.Admin.Controllers
{
    public class PostController : BaseController
    {
        private readonly IUnitOfWork _uw;
        private readonly IHostingEnvironment _env;
        private const string PostNotFound = "پستی یافت نشد.";
        private readonly IMapper _mapper;

        public PostController(IUnitOfWork uw, IMapper mapper, IHostingEnvironment env)
        {
            _uw = uw;
            _uw.CheckArgumentIsNull(nameof(_uw));

            _env = env;
            _env.CheckArgumentIsNull(nameof(_env));

            _mapper = mapper;
            _mapper.CheckArgumentIsNull(nameof(_mapper));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetPosts(string search, string order, int offset, int limit, string sort)
        {
            List<PostViewModel> posts;
            int total = _uw.BaseRepository<Post>().CountEntities();
            if (!search.HasValue())
                search = "";

            if (limit == 0)
                limit = total;

            if (sort == "ShortTitle")
            {
                if (order == "asc")
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, true, null, null, null, null, search);
                else
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, false, null, null, null, null, search);
            }

            else if (sort == "بازدید")
            {
                if (order == "asc")
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, true, null, null, null, search);
                else
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, false, null, null, null, search);
            }

            else if (sort == "لایک")
            {
                if (order == "asc")
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, true, null, null, search);
                else
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, false, null, null, search);
            }

            else if (sort == "دیس لایک")
            {
                if (order == "asc")
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, null, true, null, search);
                else
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, null, false, null, search);
            }

            else if (sort == "تاریخ انتشار")
            {
                if (order == "asc")
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, null, null, true, search);
                else
                    posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, null, null, false, search);
            }

            else
                posts = await _uw.PostRepository.GetPaginatePostAsync(offset, limit, null, null, null, null, false, search);

            if (search != "")
                total = posts.Count();

            return Json(new { total = total, rows = posts });
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrUpdate(string postId)
        {
            PostViewModel PostViewModel = new PostViewModel();
            ViewBag.Tags = _uw._Context.Tags.Select(t => t.TagName).ToList();
            PostViewModel.PostCategoriesViewModel = new PostCategoriesViewModel(_uw.CategoryRepository.GetAllCategories(), null);
            if (postId.HasValue())
            {
                var post = await (from n in _uw._Context.Post.Include(c => c.PostCategories)
                                  join w in _uw._Context.PostTags on n.PostId equals w.PostId into bc
                                  from bct in bc.DefaultIfEmpty()
                                  join t in _uw._Context.Tags on bct.TagId equals t.TagId into cg
                                  from cog in cg.DefaultIfEmpty()
                                  where (n.PostId == postId)
                                  select new PostViewModel
                                  {
                                      PostId = n.PostId,
                                      Title = n.Title,
                                      Description = n.Description,
                                      PublishDateTime = n.PublishDateTime,
                                      IsPublish = n.IsPublish,
                                      ImageName = n.ImageName,
                                      PostCategories = n.PostCategories,
                                      Url = n.Url,
                                      NameOfTags = cog != null ? cog.TagName : "",
                                  }).ToListAsync();

                if (post != null)
                {
                    PostViewModel = _mapper.Map<PostViewModel>(post.FirstOrDefault());
                    if (post.FirstOrDefault().PublishDateTime > DateTime.Now)
                    {
                        PostViewModel.FuturePublish = true;
                        PostViewModel.PersianPublishDate = post.FirstOrDefault().PublishDateTime.ConvertMiladiToShamsi("yyyy/MM/dd");
                        PostViewModel.PersianPublishTime = post.FirstOrDefault().PublishDateTime.Value.TimeOfDay.ToString();
                    }
                    PostViewModel.PostCategoriesViewModel = new PostCategoriesViewModel(_uw.CategoryRepository.GetAllCategories(), post.FirstOrDefault().PostCategories.Select(n => n.CategoryId).ToArray());
                    PostViewModel.NameOfTags = post.Select(t => t.NameOfTags).ToArray().CombineWith(',');
                }

            }

            return View(PostViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(PostViewModel viewModel, string submitButton)
        {
            ViewBag.Tags = _uw._Context.Tags.Select(t => t.TagName).ToList();
            viewModel.PostCategoriesViewModel = new PostCategoriesViewModel(_uw.CategoryRepository.GetAllCategories(), null);
            if (!viewModel.FuturePublish)
            {
                ModelState.Remove("PersianPublishTime");
                ModelState.Remove("PersianPublishDate");
            }
            if (viewModel.PostId.HasValue())
                ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                if (submitButton != "ذخیره پیش نویس")
                    viewModel.IsPublish = true;

                if (viewModel.ImageFile != null)
                    viewModel.ImageName = $"post-{StringExtensions.GenerateId(10)}.jpg";

                if (viewModel.PostId.HasValue())
                {
                    string[] newTagsArray;
                    var post = _uw.BaseRepository<Post>().FindByConditionAsync(n => n.PostId == viewModel.PostId, null, n => n.PostCategories, n => n.PostTags).Result.FirstOrDefault();
                    if (post == null)
                        ModelState.AddModelError(string.Empty, PostNotFound);
                    else
                    {
                        if (viewModel.IsPublish && post.IsPublish == false)
                            viewModel.PublishDateTime = DateTime.Now;

                        if (viewModel.IsPublish && post.IsPublish == true)
                        {
                            if (viewModel.PersianPublishDate.HasValue())
                            {
                                var persianTimeArray = viewModel.PersianPublishTime.Split(':');
                                viewModel.PublishDateTime = viewModel.PersianPublishDate.ConvertShamsiToMiladi().Date + new TimeSpan(int.Parse(persianTimeArray[0]), int.Parse(persianTimeArray[1]), 0);
                            }
                            else
                                viewModel.PublishDateTime = post.PublishDateTime;
                        }

                        if (viewModel.ImageFile != null)
                        {
                            viewModel.ImageFile.UploadFileBase64($"{_env.WebRootPath}/postImage/{viewModel.ImageName}");
                            FileExtensions.DeleteFile($"{_env.WebRootPath}/postImage/{post.ImageName}");
                        }

                        else
                            viewModel.ImageName = post.ImageName;

                        var postTags = _uw._Context.PostTags.Include(t => t.Tag).Where(t => t.PostId == viewModel.PostId).Select(t => t.Tag).ToList();
                        if (viewModel.NameOfTags.HasValue())
                        {
                            newTagsArray = viewModel.NameOfTags.Split(',');
                            viewModel.PostTags = await _uw.TagRepository.InsertPostTags(viewModel.NameOfTags.Split(','), post.PostId);
                        }
                        else
                            viewModel.PostTags = post.PostTags;

                        if (viewModel.CategoryIds == null)
                            viewModel.PostCategories = post.PostCategories;
                        else
                            viewModel.PostCategories = viewModel.CategoryIds.Select(c => new PostCategory { CategoryId = c, PostId = post.PostId }).ToList();

                        viewModel.UserId = post.UserId;
                        _uw.BaseRepository<Post>().Update(_mapper.Map(viewModel, post));
                        await _uw.Commit();
                        ViewBag.Alert = "ذخیره تغییرات با موفقیت انجام شد.";
                        viewModel.PostCategoriesViewModel.CategoryId = viewModel.CategoryIds;
                    }
                }

                else
                {
                    viewModel.ImageFile.UploadFileBase64($"{_env.WebRootPath}/postImage/{viewModel.ImageName}");
                    viewModel.PostId = StringExtensions.GenerateId(10);
                    viewModel.UserId = User.Identity.GetUserId<int>();

                    if (viewModel.IsPublish)
                    {
                        if (!viewModel.PersianPublishDate.HasValue())
                            viewModel.PublishDateTime = DateTime.Now;
                        else
                        {
                            var persianTimeArray = viewModel.PersianPublishTime.Split(':');
                            viewModel.PublishDateTime = viewModel.PersianPublishDate.ConvertShamsiToMiladi().Date + new TimeSpan(int.Parse(persianTimeArray[0]), int.Parse(persianTimeArray[1]), 0);
                        }
                    }

                    if (viewModel.CategoryIds != null)
                        viewModel.PostCategories = viewModel.CategoryIds.Select(c => new PostCategory { CategoryId = c }).ToList();
                    else
                        viewModel.PostCategories = null;

                    if (viewModel.NameOfTags.HasValue())
                        viewModel.PostTags = await _uw.TagRepository.InsertPostTags(viewModel.NameOfTags.Split(","));
                    else
                        viewModel.PostTags = null;

                    await _uw.BaseRepository<Post>().CreateAsync(_mapper.Map<Post>(viewModel));
                    await _uw.Commit();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(viewModel);
        }


        [HttpGet, AjaxOnly]
        public async Task<IActionResult> Delete(string postId)
        {
            if (!postId.HasValue())
                ModelState.AddModelError(string.Empty, PostNotFound);
            else
            {
                var post = await _uw.BaseRepository<Post>().FindByIdAsync(postId);
                if (post == null)
                    ModelState.AddModelError(string.Empty, PostNotFound);
                else
                    return PartialView("_DeleteConfirmation", post);
            }
            return PartialView("_DeleteConfirmation");
        }


        [HttpPost, ActionName("Delete"), AjaxOnly]
        public async Task<IActionResult> DeleteConfirmed(Post model)
        {
            if (model.PostId == null)
                ModelState.AddModelError(string.Empty, PostNotFound);
            else
            {
                var post = await _uw.BaseRepository<Post>().FindByIdAsync(model.PostId);
                if (post == null)
                    ModelState.AddModelError(string.Empty, PostNotFound);
                else
                {
                    _uw.BaseRepository<Post>().Delete(post);
                    await _uw.Commit();
                    FileExtensions.DeleteFile($"{_env.WebRootPath}/postImage/{post.ImageName}");
                    TempData["notification"] = DeleteSuccess;
                    return PartialView("_DeleteConfirmation", post);
                }
            }
            return PartialView("_DeleteConfirmation");
        }


        [HttpPost, ActionName("DeleteGroup"), AjaxOnly]
        public async Task<IActionResult> DeleteGroupConfirmed(string[] btSelectItem)
        {
            if (btSelectItem.Count() == 0)
                ModelState.AddModelError(string.Empty, "هیچ پستی برای حذف انتخاب نشده است.");
            else
            {
                foreach (var item in btSelectItem)
                {
                    var post = await _uw.BaseRepository<Post>().FindByIdAsync(item);
                    _uw.BaseRepository<Post>().Delete(post);
                    FileExtensions.DeleteFile($"{_env.WebRootPath}/postImage/{post.ImageName}");
                }
                await _uw.Commit();
                TempData["notification"] = "حذف گروهی اطلاعات با موفقیت انجام شد.";
            }

            return PartialView("_DeleteGroup");
        }
    }
}