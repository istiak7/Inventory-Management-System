﻿using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_System.Models
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ? UpdatedAt { get; set; }
    }
}
        