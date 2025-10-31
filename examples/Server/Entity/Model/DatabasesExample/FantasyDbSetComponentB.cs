﻿using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    //[FantasyDbSet("ExampleComponentB", Description = "Fantasy Table Test Component")]
    public class FantasyDbSetComponentB : Entity
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}
