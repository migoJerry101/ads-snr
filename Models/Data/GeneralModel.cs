namespace ads.Models.Data
{
    public class GeneralModel
    {
        //TBLSTR
        public string? STRNUM { get; set; }

        //INVMST
        public string? INUMBR { get; set; }

        //CSHDET
        public string? CSDATE { get; set; }
        public decimal CSQTY { get; set; }
        public string? CSSTOR { get; set; }
        public string? CSSKU { get; set; }

        //INVBAL    
        public decimal IBHAND { get; set; }
        public string? INUMBR2 { get; set; }
        public string? ISTORE { get; set; }
    }
}
