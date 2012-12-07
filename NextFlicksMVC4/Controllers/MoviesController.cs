﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NextFlicksMVC4;
using NextFlicksMVC4.Models;
using NextFlicksMVC4.NetFlixAPI;
using System.Timers;

namespace NextFlicksMVC4.Controllers
{
    public class MoviesController : Controller
    {
        public static MovieDbContext db = new MovieDbContext();

        //
        // GET: /Movies/

        [HttpGet]
        public ActionResult Filter()
        {

            //grab the lowest year in Catalog
            var min_qry = "select  top(1) * from Movies where year != \'\' order by year ASC";
            var min_res = db.Movies.SqlQuery(min_qry);
            var min_list = min_res.ToList();
            string min_year = min_list[0].year;
            ViewBag.min_year = min_year;
            //grab the highest year in catalog
            var max_qry = "select  top(1) * from Movies order by year DESC ";
            var max_res = db.Movies.SqlQuery(max_qry);
            var max_list = max_res.ToList();
            string max_year = max_list[0].year;
            ViewBag.max_year = max_year;

            //Create a list of SelectListItems
            List<SelectListItem> years = new List<SelectListItem>();
            for (int i = Int32.Parse(min_year); i <= Int32.Parse(max_year); i++)
            {
                years.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }

            //Create a selectList, but since it's looking for an IEnumerable,
            // tell it what is the value and text parts
            SelectList slist = new SelectList(years, "Value", "Text" );
            
            //give the Viewbag a property for the SelectList
            ViewBag.years = slist;

            return View();
        }

        //[HttpPost]
        //public ActionResult Filter(string name)
        //{
         

        //    return View("FilterHandler");
        //}


        public ActionResult FilterHandler(string year_start, string year_end)
        {
            ViewData["year_start"] = year_start;
            ViewData["year_end"] = year_end;
            Trace.WriteLine(ViewData["pet"]);

            return View("FilterHandler");
        }

        /// <summary>
        /// Sloppy return random set of movies. redirects to Index with a random start and count of 10
        /// </summary>
        /// <returns></returns>
        public ActionResult Random()
        {
            int rand_title_int = new Random().Next(1, db.Movies.ToList().Count);
            return RedirectToAction("Index", new {count = 1, start = rand_title_int});
        }


        public ActionResult Test()
        {
            return View(@"~/Views/Home/About.cshtml");
        }




        public ActionResult Year(int year_start = 2001, int year_end = 2002, int start = 0, int count = 25, bool is_movie = true)
        {
            //returns all titles from year
            string qry = "select * from Movies where (year between {0} and {1}) and (is_movie = {2}) order by year";
            var res = db.Movies.SqlQuery(qry, year_start, year_end, is_movie);

            List<Movie> movie_list = res.ToList();

            ViewBag.TotalMovies = movie_list.Count -1;
            ViewBag.year_start = year_start;
            ViewBag.year_end = year_end;
            ViewBag.start = start;
            ViewBag.count = count;

            if (count > movie_list.Count)
            {
                count = movie_list.Count - 1;
            }
            var results = movie_list.GetRange(start, start + count);
            return View("Index", results);
        }

        public ActionResult Index(int start = 0, int count = 10)
        {
            var db = new MovieDbContext();
            Trace.WriteLine("To QRY");
            //var fullList = db.Movies.ToList();
            string qry = "select * from" +
                         " ( select " +
                         "  ROW_NUMBER() over (order by movie_id) as rownum," +
                         " *" +
                         " from Movies) foo" +
                         " where rownum  between {0} and {1}";
            var res = db.Movies.SqlQuery(qry, start, start+count);
            Trace.WriteLine("To list");
            var fullList = res.ToList();
            
            //total movies in DB
            ViewBag.TotalMovies = fullList.Count;
            ViewBag.Start = start;
            ViewBag.Count = count;

            //make sure there's not a outofbounds
            if (count > fullList.Count)
            {
                count = fullList.Count;
                Trace.WriteLine("had to shorten the returned results");
            }

            Trace.WriteLine("Get ranging");
            var full_range = fullList.GetRange(0, count);

            Trace.WriteLine("Returning View");
            return View(full_range);
        }

        public ActionResult Table(int start = 0, int count = 10)
        {
            Trace.WriteLine("To QRY");
            //var fullList = db.Movies.ToList();
            string qry = "select * from" +
                         " ( select " +
                         "  ROW_NUMBER() over (order by movie_ID) as rownum," +
                         " *" +
                         " from Movies) foo" +
                         " where rownum  between {0} and {1}";
            var res = db.Movies.SqlQuery(qry, start, start + count);
            Trace.WriteLine("To list");
            var fullList = res.ToList();

            //total movies in DB
            ViewBag.TotalMovies = fullList.Count;
            ViewBag.Start = start;
            ViewBag.Count = count;

            Trace.WriteLine("Get ranging");
            var full_range = fullList.GetRange(0, count);

            Trace.WriteLine("Returning View");
            return View(full_range);
            
        }

        public ActionResult Full()
        {

            Trace.WriteLine("starting Full Action");
            string msg = DateTime.Now.ToShortTimeString();
            var start_time = DateTime.Now;
            Trace.WriteLine(msg);
            var db = new MovieDbContext();
            //------------------------------------------------------

            //need to have a Genre table first, so make sure that's there
            //int genre_count = db.Genres.Count();
            //if (genre_count == 0)
            //{
                
            //}

            PopulateGenres.PopulateGenresTable();


            //------------------------------------------------------



            Trace.WriteLine("starting data read");
             msg = DateTime.Now.ToShortTimeString();
            Trace.WriteLine(msg);

            // Go line by line, and parse it for Movie files
            List<Movie> listOfMovies = new List<Movie>();
            string data;
            int count = 0;
            using (StreamReader reader = new StreamReader(@"C:\fixedAPI.NFPOX"))
            {

                Trace.WriteLine("Starting to read");
                
                data = reader.ReadLine();
                try
                {
                    while (data != null)
                    {
                        if (!data.StartsWith("<catalog_title>"))
                        {
                            Trace.WriteLine("Invalid line");
                        }
                        else
                        {
                            //parse line for a title, which is what NF returns
                            List<Title> titles =
                                NextFlicksMVC4.Create.ParseXmlForCatalogTitles(data);
                            Movie movie =
                                NextFlicksMVC4.Create.CreateMovie(titles[0]);
                            listOfMovies.Add(movie);
                            db.Movies.Add(movie);
                            db.SaveChanges();
                            

                            //add boxart and genre data to db before saving the movie 
                            BoxArt boxArt = NextFlicksMVC4.Create.CreateMovieBoxartFromTitle(movie,
                                                        titles[0]);
                            db.BoxArts.Add(boxArt);

                            //genres to database
                            foreach (Genre genre in titles[0].ListGenres)
                            {
                                MovieToGenre movieToGenre =
                                    NextFlicksMVC4.Create.CreateMovieMovieToGenre(movie,
                                                                                  genre);
                                db.MovieToGenres.Add(movieToGenre);
                                db.SaveChanges();
                                var save_msg =
                                    String.Format(
                                        "done saving MtG mtg_id = {0}\n movie_id = {1}",
                                        movieToGenre.movie_to_genre_ID,
                                        movieToGenre.movie_ID);
                                        
                            Trace.WriteLine(save_msg);
                            }


                            //log adding data
                             msg = String.Format("Added item {0} to database, moving to next one", count.ToString());
                            Trace.WriteLine(msg);
                            count += 1;

                        }
                        data = reader.ReadLine();
                    }
                }

                catch (System.Xml.XmlException ex)
                {
                    Trace.WriteLine("Done parsing the XML because of something happened. Probably the end of file:");
                    Trace.WriteLine(ex.Message);
                }

                //Trace.WriteLine("Beginning Add to DB");

                //set up checkpoints for progress updates
                //int modulo =0;
                //List<int> checkpoints = new List<int>();
                //int total = listOfMovies.Count;
                //int start = total/25;
                //if (start == 0)
                //{
                //    start = 1;
                //}
                //while (modulo <= listOfMovies.Count)
                //{
                //   checkpoints.Add(modulo);
                //    modulo += start;
                //}

                //go through list of movies and add to database
                //int counter = 0;
                //if (listOfMovies.Count > 0)
                //{
                //    foreach (Movie movie in listOfMovies)
                //    {
                //        //db.Movies.Add(movie);
                        

                //        //counting stuff, not logic essential
                //        counter += 1;
                //        if (checkpoints.Contains(counter))
                //        {
                //            string msg =
                //                String.Format(
                //                    "Done adding at least {0} movies", counter);
                //            Trace.WriteLine(msg);
                //        }
                //    }

                    Trace.WriteLine("Saving Changes any untracked ones, anyways");
                    db.SaveChanges();
                    Trace.WriteLine("Done Saving! Check out Movies/index for a table of the stuff");

                //}
            }


            Trace.WriteLine("Done everything");
             msg = DateTime.Now.ToShortTimeString();
            Trace.WriteLine(msg);
            var end_time = DateTime.Now;

            TimeSpan span = end_time - start_time;
            Trace.WriteLine("It took this long:");
            Trace.WriteLine(span);

            return View();
        }

        public  ActionResult API(string term="Jim Carrey")
        {
            //grab new movies, turn one into a Movie and view it
            var data = OAuth1a.GetNextflixCatalogDataString("catalog/titles/streaming", term, max_results:"100", outputPath:@"C:/streamingAPI2.NFPOX");
            var titles =
                NextFlicksMVC4.Create.ParseXmlForCatalogTitles(data);

            List<Movie> movies = new List<Movie>();

            foreach (Title title in titles)
            {
                Movie movie = NextFlicksMVC4.Create.CreateMovie(title);
                movies.Add(movie);
                db.Movies.Add(movie);

                BoxArt boxArt = NextFlicksMVC4.Create.CreateMovieBoxartFromTitle(movie,
                                                                        title);
                db.BoxArts.Add(boxArt);
                foreach (Genre genre in title.ListGenres)
                {
                    MovieToGenre movieToGenre =
                        NextFlicksMVC4.Create.CreateMovieMovieToGenre(movie,
                                                                      genre);
                    db.MovieToGenres.Add(movieToGenre);

                }
            }

            db.SaveChanges();

            return View(movies.ToList());
        }

        //
        // GET: /Movies/Details/5

        public ActionResult Details(int movie_ID = 0)
        {
            var db = new MovieDbContext();

            Movie movie = db.Movies.Find(movie_ID);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        //
        // GET: /Movies/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Movies/Create

        [HttpPost]
        public ActionResult Create(Movie movie)
        {
            if (ModelState.IsValid)
            {
                db.Movies.Add(movie);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(movie);
        }

        //
        // GET: /Movies/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Movie movie = db.Movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        //
        // POST: /Movies/Edit/5

        [HttpPost]
        public ActionResult Edit(Movie movie)
        {
            if (ModelState.IsValid)
            {
                db.Entry(movie).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(movie);
        }

        //
        // GET: /Movies/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Movie movie = db.Movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        //
        // POST: /Movies/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Movie movie = db.Movies.Find(id);
            db.Movies.Remove(movie);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}