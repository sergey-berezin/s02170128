﻿using System.Collections.Generic;
using System.IO;
using ImageRecognition;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;

namespace Contracts
{
    public class DbImageClass
    {
        [Key]
        public int ImageClassID { get; set; }
        public string ClassName { get; set; }

        public virtual ICollection<DbImage> Images { get; set; }

        public DbImageClass(string ClassName="")
        {
            Images = new Collection<DbImage>();
            this.ClassName = ClassName;
        }
    }


    public class DbImage
    {
        [Key]
        public int ImageID { get; set; }
        public float Proba { get; set; }
        public string FilePath { get; set; }
        public virtual DbImageDetails ImageDetails { get; set; }

        public virtual int ImageClassID { get; set; }
        public virtual DbImageClass ImageClass { get; set; }

        public DbImage() { }
        public DbImage(PredictionResult pr)
        {
            this.Proba = pr.Proba;
            this.FilePath = pr.FilePath;
            this.ImageDetails = new DbImageDetails(File.ReadAllBytes(pr.FilePath));
        }
       
    }

    public class DbImageDetails
    {
        public int DbImageDetailsID {get; set;}
        public byte[] ImageData { get; set; }
        public DbImageDetails() { }
        public DbImageDetails(byte[] ImageData)
        {
            this.ImageData = ImageData;
        }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<DbImage> Images { get; set; }
        public DbSet<DbImageClass> Classes { get; set; }
        public DbSet<DbImageDetails> Details { get; set; }

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbImage>()
                .HasOne(p => p.ImageClass)
                .WithMany(b => b.Images)
                .HasForeignKey(p => p.ImageClassID);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=helloappdb;Trusted_Connection=True;");
        }

    }
}
