// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Zerra.Repository;

namespace Zerra.TestDev
{
    [Entity("Customer")]
    public class TestCustomerEntityFrameworkModel
    {
        [Identity]
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }

        [Relation(nameof(CustomerID))]
        public TestOrderEntityFrameworkModel[] Orders { get; set; }
    }

    [Entity("Order")]
    public class TestOrderEntityFrameworkModel
    {
        [Identity]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Date { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }
    }



    public class TestEntityFrameworkContext : DbContext
    {
        public DbSet<CustomerEntity> Customers { get; set; }
        public DbSet<OrderEntity> Orders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlServer(@"Server=.;Database=Test;Integrated Security=True");
        }
    }

    [Table("Customer")]
    public class CustomerEntity
    {
        [Key]
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }
        public IList<OrderEntity> Orders { get; set; }
    }

    [Table("Order")]
    public class OrderEntity
    {
        [Key]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime Date { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }
    }
}
