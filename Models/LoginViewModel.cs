using System.ComponentModel.DataAnnotations;

namespace JobApply.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o nome de usuário.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;
    }
}