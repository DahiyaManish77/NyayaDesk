using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace NyayaDesk.Models.ViewModels
{
    public class WorkspaceRegistrationVM
    {
        [Required,StringLength(100)] public string FirstName { get; set; }
        [StringLength(100)] public string LastName { get; set; }
        [Required,EmailAddress,StringLength(200)] public string Email { get; set; }
        [Required,RegularExpression(@"^[0-9+()\-\s]{10,15}$",ErrorMessage="Enter a valid mobile number.")] public string MobileNumber { get; set; }
        [Required,StringLength(180)] [Display(Name="Law firm / workspace name")] public string WorkspaceName { get; set; }
        [Required] [Display(Name="I am registering as")] public string ProfessionalRole { get; set; }
        [Required] public string PlanCode { get; set; }
        [Required,StringLength(100,MinimumLength=8)] [DataType(DataType.Password)] public string Password { get; set; }
        [Required,System.ComponentModel.DataAnnotations.Compare("Password")] [DataType(DataType.Password)] public string ConfirmPassword { get; set; }
        [Range(typeof(bool),"true","true",ErrorMessage="Accept the terms and privacy policy to continue.")] public bool AcceptTerms { get; set; }
    }
}
