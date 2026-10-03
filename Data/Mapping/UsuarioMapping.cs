using JobApply.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApply.Data.Mappings
{
    public class UsuarioMapping
        : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(
            EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedOnAdd();

            builder.Property(u => u.Nome)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(150)
                .IsRequired(false);

            builder.Property(u => u.SenhaHash)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Ativo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.DataCadastro)
                .IsRequired()
                .HasDefaultValueSql(
                    "SYSUTCDATETIME()"
                );

            builder.Property(u => u.Telefone)
                .HasMaxLength(30)
                .IsRequired(false);

            builder.Property(u => u.Pais)
                .HasMaxLength(2)
                .IsRequired(false);
        }
    }
}