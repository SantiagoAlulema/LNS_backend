namespace back_lns_libros.Clases
{
    public class StudentBookSave
    {
        public string codigolibro {get; set;}
        public string codigoestudiante { get; set; }
        public string periodo { get; set; }
        public string ciclo { get; set; }
        public string aceptaterminos { get; set; }
        public string unidadeducativa { get; set; }
        public string nombreestudiante { get; set; }
        public string serie { get; set; }
        public bool noexisteunidadeducativa { get; set; } = false;
        public string latitud { get; set; }
        public string longitud { get; set; }



    }
}
