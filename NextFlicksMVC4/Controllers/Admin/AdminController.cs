﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NextFlicksMVC4.Models;
using NextFlicksMVC4.NetFlixAPI;
using NextFlicksMVC4.OMBD;

namespace NextFlicksMVC4.Controllers.Admin
{
    [Authorize(Roles="Admin")]
    public class AdminController : Controller
    {
        //
        // GET: /Admin/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DbTools()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DbTools(string button)
        {
            if (button == "Drop Tables")
            {
                DatabaseTools.DropTables();
                ViewBag.Message = "Tables Dropped";
            }
            if (button == "Create Tables")
            {
                DatabaseTools.CreateTables();
                ViewBag.Message = "Tables Created";
            }
            if (button == "Drop And Create")
            {
                DatabaseTools.DropAndCreate();
                ViewBag.Message = "Tables dropped and Recreated";
            }
            if (button == "Full")
            {
                DatabaseTools.Full();
                ViewBag.Message = "Full Db Created";
            }
            if (button == "Api")
            {
                DatabaseTools.Api();
                ViewBag.Message = "Api Downloaded.";
            }
            if (button == "Get Genres From Netflix")
            {
                DatabaseTools.NetflixGenres();
                ViewBag.Message = "Genres downloaded fron Netflix";
            }
            if (button == "Update Genres In DB")
            {
                DatabaseTools.UpdateGenreList(System.Web.HttpContext.Current.Server.MapPath("~/dbfiles/fixedAPI.NFPOX"));
                ViewBag.Message = "Update Genres List";

            }
            if (button == "Full Update")
            {
                DatabaseTools.FullDbBuild();
            }
            if (button == "Join Lines")
            {
                Tools.JoinLines(System.Web.HttpContext.Current.Server.MapPath("~/dbfiles/fixedAPI.NFPOX"));
            }
            if (button == "omdb")
            {
                Omdb.DownloadOmdbZipAndExtract(System.Web.HttpContext.Current.Server.MapPath("~/dbfiles/omdb.zip"));
            }
            if (button == "hash")
            {
                DatabaseTools.RemoveDuplicateMovies();
            }
            return View();
        }
    }
}
