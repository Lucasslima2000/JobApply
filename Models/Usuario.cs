namespace JobApply.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string SenhaHash { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public DateTime DataCadastro { get; set; } =
            DateTime.UtcNow;

        public string? Telefone { get; set; }

        public string? Pais { get; set; }
    }
}