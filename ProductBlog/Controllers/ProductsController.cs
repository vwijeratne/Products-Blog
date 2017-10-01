using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ProductBlog.Models;
using PagedList;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ProductBlog.Controllers
{
    public class ProductsController : Controller
    {
        private VBlogEntities db = new VBlogEntities();

        // GET: Products
        public ActionResult Index(string sortOrder, int? page)
        {       
            //Stores the active sorting order
            ViewBag.CurrentSort = sortOrder;
            //Setting the sorting order(s) in to the ViewBag, to change between sorting of different columns
            ViewBag.ProductnameSortParm = String.IsNullOrEmpty(sortOrder) ? "pname_desc" : "";
            ViewBag.ProductSKUSortParm = sortOrder == "SKU" ? "sku_desc" : "SKU";
            ViewBag.ProductPriceSortParm = sortOrder == "Price" ? "price_desc" : "Price";

            var products = from p in db.Products
                           select p;

            foreach (Product p in products)
            {
                if(Directory.Exists(Server.MapPath("~/UploadedFiles/" + p.ProductID)))
                    p.AttachmentsAvailable = Directory.GetFiles(Server.MapPath("~/UploadedFiles/" + p.ProductID)).Length == 0 ? false : true;
            }

            //Sort the products list based on the sorting selected
            switch (sortOrder)
            {
                case "pname_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "SKU":
                    products = products.OrderBy(p => p.SKU);
                    break;
                case "sku_desc":
                    products = products.OrderByDescending(p => p.SKU);
                    break;
                case "Price":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            int pageSize = 10;
            //The two question marks represent the null-coalescing operator. 
            //The null-coalescing operator defines a default value for a nullable type, the expression (page ?? 1) means return the value of page if it has a value, or return 1 if page is null.
            int pageNumber = (page ?? 1);
            //ToPagedList extension method on the products IQueryable object converts the product query to a single page of products in a collection type that supports paging. 
            return View(products.ToPagedList(pageNumber, pageSize));

            //return View(products.ToList());
            //return View(db.Products.ToList());
        }

        //Create the thumbnail of the product image uploaded
        public FileContentResult GetThumbnailImage(int productId)
        {
            //Get the product by product id
            Product productThumbnail = db.Products.FirstOrDefault(p => p.ProductID == productId);
            //Returns the thumbnail image
            if (productThumbnail != null && productThumbnail.ImageThumbnail != null && productThumbnail.ImageMimeType != null)
            {
                return File(productThumbnail.ImageThumbnail, productThumbnail.ImageMimeType.ToString());
            }
            else
            {
                return null;
            }
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductID,ProductName,SKU,Price")] Product product, HttpPostedFileBase image, HttpPostedFileBase[] ProductFiles)
        {
            if (ModelState.IsValid)
            {
                if (image != null)
                {
                    //attach the uploaded image to the object before saving to Database
                    product.ImageMimeType = image.ContentLength;
                    product.ImageData = new byte[image.ContentLength];
                    image.InputStream.Read(product.ImageData, 0, image.ContentLength);

                    //Create thumbnail
                    using (var srcImage = Image.FromStream(image.InputStream, true, true)) 
                    using (var newImage = new Bitmap(100, 100))
                    using (var graphics = Graphics.FromImage(newImage))
                    using (var stream = new MemoryStream())
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(srcImage, new Rectangle(0, 0, 50, 50));
                        newImage.Save(stream, ImageFormat.Png);
                        var thumbNew = File(stream.ToArray(), "image/png");
                        product.ImageThumbnail = thumbNew.FileContents;
                    }
                }                

                db.Products.Add(product);
                db.SaveChanges();

                if (ProductFiles.Count() > 0)
                {
                    foreach (HttpPostedFileBase file in ProductFiles)
                    {
                        string _FileName = Path.GetFileName(file.FileName);
                        string folderPath = Server.MapPath("~/UploadedFiles/" + product.ProductID);
                        string filePath = Path.Combine(Server.MapPath("~/UploadedFiles/" + product.ProductID), _FileName);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        file.SaveAs(filePath);
                    }                    
                }

                int newPK = product.ProductID;
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,SKU,Price,ImageData,ImageMimeType,ImageThumbnail")] Product product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        //// POST: Products/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Product product = db.Products.Find(id);
        //    db.Products.Remove(product);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

        public ActionResult DeleteConfirmed(FormCollection form)
        {
            List<int> allIdsToRemove = form["ProductChk"] != null
            ? form["ProductChk"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList() : new List<int>();

            List<Product> products = new List<Product>();
            foreach (int id in allIdsToRemove)
            {
                products.Add(db.Products.Find(id));
            }

            db.Products.RemoveRange(products);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ViewAttachments(int productId)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Server.MapPath("~/UploadedFiles/" + productId));
            List<FileInfo> files = dirInfo.GetFiles().ToList();

            //pass the data trough the "View" method
            return View(files);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
