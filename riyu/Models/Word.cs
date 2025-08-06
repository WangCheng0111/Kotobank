using System;
using System.ComponentModel.DataAnnotations;

namespace riyu.Models;

public class Word
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Japanese { get; set; } = string.Empty;
    
    [Required]
    public string Chinese { get; set; } = string.Empty;
    
    public string PartOfSpeech { get; set; } = string.Empty;
}