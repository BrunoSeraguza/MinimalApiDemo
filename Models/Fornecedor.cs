using System.Reflection.Metadata;

namespace MinimalApiDemo.Models
{
    public class Fornecedor
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public Documento Documentos { get; set; }
        public bool Ativo;

        public enum Documento
        {
            Cpf, 
            Cnpj  
        }
    }
}
