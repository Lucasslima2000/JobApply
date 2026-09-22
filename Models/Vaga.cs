using System.ComponentModel.DataAnnotations;

namespace JobApply.Models
{
    public class Vaga
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o título da vaga.")]
        [StringLength(200, ErrorMessage = "O título pode ter no máximo 200 caracteres.")]
        [Display(Name = "Título da vaga")]
        public string Titulo { get; set; } = "";

        [Required(ErrorMessage = "Informe a empresa.")]
        [StringLength(200, ErrorMessage = "A empresa pode ter no máximo 200 caracteres.")]
        public string Empresa { get; set; } = "";

        [Required(ErrorMessage = "Informe a localização.")]
        [StringLength(200, ErrorMessage = "A localização pode ter no máximo 200 caracteres.")]
        [Display(Name = "Localização")]
        public string Localizacao { get; set; } = "";

        [Required(ErrorMessage = "Informe o link da vaga.")]
        [Url(ErrorMessage = "Informe um link válido.")]
        [StringLength(500, ErrorMessage = "O link pode ter no máximo 500 caracteres.")]
        [Display(Name = "Link da vaga")]
        public string Link { get; set; } = "";

        [Required(ErrorMessage = "Selecione um status.")]
        public string Status { get; set; } = "Interessado";
    }
}