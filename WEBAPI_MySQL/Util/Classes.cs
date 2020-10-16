using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;


namespace daniel_api
{
    public enum Tipo_Conexao
    {
        ActiveDirectory,
        SemActiveDirectory,
    }
    public class Tabela
    {
        public string ma { get; set; } = "";
        public string s { get; set; } = "";
        public int id_user { get; set; } = 0;
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string Mensagem { get; set; } = "";
        public string GetJSON()
        {
            string p = "{" +
                "\n  " + Utilz.aspas + "Status" + Utilz.aspas + ":" + Utilz.aspas + this.Status.Replace("\n", " ") + Utilz.aspas + "," +
                "\n  " + Utilz.aspas + "id_user" + Utilz.aspas + ":" + Utilz.aspas + this.id_user + Utilz.aspas + "," +
                "\n  " + Utilz.aspas + "Nome" + Utilz.aspas + ":" + Utilz.aspas + this.Nome + Utilz.aspas + "," +
                "\n  " + Utilz.aspas + "Email" + Utilz.aspas + ":" + Utilz.aspas + this.Email + Utilz.aspas + "," +
                "\n  " + Utilz.aspas + "Mensagem" + Utilz.aspas + ":" + Utilz.aspas +  this.Mensagem.Replace("\n", " ") + Utilz.aspas + "," +
                "\n  " + Utilz.aspas + "Resultados" + Utilz.aspas + ":" + Utilz.aspas +  this.Linhas.Count + Utilz.aspas + "," +
               "\n  " + Utilz.aspas + "Valores" + Utilz.aspas + ": \n  [\n";
            for (int i = 0; i < Linhas.Count; i++)
            {
                p = p + (i>0? ",\n":"")+ Linhas[i].GetJSON();
            }
            p = p + "\n  ]\n}";
            return p;
        }
        public string Status { get; set; } = "";
        public int Resultados { get; set; } = 0;
        public List<Registro> Linhas { get; set; } = new List<Registro>();
        public List<Registro> Filtrar(string Chave, string Valor, bool exato = false)
        {
            List<Registro> Retorno = new List<Registro>();
            if (exato)
            {
                return Linhas.FindAll(x => x.Valores.FindAll(y => y.Coluna == Chave && y.Valor == Valor).Count > 0);
            }
            else
            {
                return Linhas.FindAll(x => x.Valores.FindAll(y => y.Coluna == Chave && y.Valor.ToLower().Contains(Valor.ToLower())).Count > 0);
            }
        }
        public Tabela()
        {

        }

        public Tabela(Registro filtro)
        {
            this.Email = filtro.Email;
            this.id_user = filtro.id_user;
            this.Mensagem = filtro.Mensagem;
            this.Nome = filtro.Nome;
            this.Status = filtro.Status;
            this.ma = filtro.ma;
            this.s = filtro.s;
        }
        public Tabela(List<Registro> Linhas, string status)
        {
            this.Linhas = Linhas;
            this.Resultados = this.Linhas.Count;
            this.Status = status;
        }
    }

    public class Registro
    {
        public int id_user { get; set; } = 0;
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string Mensagem { get; set; } = "";

        public string ma { get; set; } = "";
        public string s { get; set; } = "";
        public string Status { get; set; } = "";
        public string Tabela { get; set; } = "";
        public string Banco { get; set; } = "";
        public string GetJSON()
        {
            string p = "    {\n";
            for (int i = 0; i < Valores.Count; i++)
            {
                p = p + (i>0? ",\n":"") + "      " + Valores[i].GetJSON();
            }
            p = p + "\n    }";

            return p;
        }
        public Celula Get(string coluna)
        {
            var celula = this.Valores.Find(x => x.Coluna.ToUpper().Replace(" ", "") == coluna.ToUpper().Replace(" ", ""));
            if(celula!=null)
            {
                return celula;
            }
            return new Celula();
        }
        public void Set(string coluna, string valor)
        {
            var celula = this.Valores.Find(x => x.Coluna.ToUpper().Replace(" ", "") == coluna.ToUpper().Replace(" ", ""));
            if(celula!=null)
            {
                celula.Valor = valor;
            }
        }
        public List<Celula> Valores { get; set; } = new List<Celula>();
        public List<Celula> Filtros { get; set; } = new List<Celula>();

        public Registro(string Tabela, List<string> Colunas, MySqlDataReader sqlDataReader)
        {

            foreach (string Coluna in Colunas)
            {
                Celula n = new Celula(Coluna, sqlDataReader[Coluna].ToString());
                this.Valores.Add(n);
            }
        }

        public Registro()
        {

        }

        public Registro(JObject value)
        {
  

            try
            {
                this.Banco = value.GetValue("Banco").ToString();
                this.Tabela = value.GetValue("Tabela").ToString();
                var valores = value.GetValue("Valores");
                var filtro = value.GetValue("Filtros");
                this.ma = value.GetValue("ma").ToString();
                this.s = value.GetValue("s").ToString();
                if (valores!=null)
                {
                    if (valores.Children().ToList().Count > 0)
                    {
                        foreach (JProperty p in valores.Children().ToList())
                        {
                            this.Valores.Add(new Celula(p.Name, p.Value));
                        }
                    }
                }
              
                if(filtro!=null)
                {
                    if (filtro.Children().ToList().Count > 0)
                    {
                        foreach (JProperty p in filtro.Children().ToList())
                        {
                            this.Filtros.Add(new Celula(p.Name, p.Value));
                        }
                    }
                }
              
            }
            catch (Exception ex)
            {
                this.Status = ex.Message + ", " + ex.StackTrace;
        
            }

            this.Status = "OK";
        }
    }
    public class Celula
    {
        public string GetJSON()
        {
            //"localidade": "São Paulo"
            return Utilz.aspas + this.Coluna + Utilz.aspas + ": " + Utilz.aspas + this.Valor + Utilz.aspas;
        }
        public string Serializar()
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            jsonString = JsonSerializer.Serialize(this, options);
            return jsonString;
        }
        public void Set(string valor)
        {
            this.Valor = valor;
        }
        public void Set(double valor)
        {
            this.Valor = valor.ToString().Replace(",", ".");
        }
        public void Set(int valor)
        {
            this.Valor = valor.ToString().Replace(",", ".");
        }
        public void Set(bool valor)
        {
            this.Valor = valor.ToString().Replace(",", ".");
        }
        public void Set(DateTime valor)
        {
            this.Valor = valor.ToShortDateString();
        }
        public Valor Get()
        {
            return new Valor(Valor);
        }
        public override string ToString()
        {
            return Valor;
        }
        public string Coluna { get; set; } = "";
        public string Valor { get; set; } = "";

        public double Double(int Decimais = 4)
        {
            if (Valor == null)
            {
                return 0;
            }
            System.Globalization.CultureInfo US = new System.Globalization.CultureInfo("en-US");
            System.Globalization.CultureInfo BR = new System.Globalization.CultureInfo("pt-BR");
            
            try
            {

                double val;
                if (double.TryParse(Valor.ToString().Replace(" ", "").Replace("%", "").Replace("@", "").Replace("#", ""), System.Globalization.NumberStyles.Float, BR, out val))
                {
                    try
                    {
                        return Math.Round(val, Decimais);

                    }
                    catch (Exception)
                    {

                        return val;
                    }
                }

                else if (double.TryParse(Valor.ToString().Replace(" ", "").Replace("%", "").Replace("@", "").Replace("#", ""), System.Globalization.NumberStyles.Float, US, out val))
                {
                    try
                    {

                        return Math.Round(val, Decimais);
                    }
                    catch (Exception)
                    {
                        return val;
                    }
                }
                else return 0;
            }
            catch (Exception)
            {

                return 0;
            }


        }
        public long Long()
        {


            if (Valor == null) { return 0; }
            string comps = Valor.ToString();
            if (comps == "") { comps = "0"; }
            try
            {
                return Convert.ToInt64(comps.Replace(".", ","));
            }
            catch (Exception ex)
            {

                return 0;
            }

        }
        public  int Int()
        {

            if (Valor == null) { return 0; }
            string comps = Valor.ToString().Replace(" ", "");
            if (comps == "") { comps = "0"; }
            try
            {
                return Convert.ToInt32(comps.Replace(".", ","));
            }
            catch (Exception ex)
            {

                return 0;
            }

        }
        public bool Boolean()
        {
            if (Valor == null)
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(Valor);
            }
            catch (Exception)
            {

                return false;
            }
        }

        public Celula()
        {

        }

        public Celula(string Valor)
        {
            this.Valor = Valor;
            this.Coluna = Coluna;
        }
        public Celula(string Coluna, string Valor)
        {
            this.Valor = Valor;
            this.Coluna = Coluna;
        }

        public Celula(string Coluna, double Valor)
        {
            this.Valor = Valor.ToString().Replace(",", ".");
            this.Coluna = Coluna;
        }
        public Celula(string Coluna, int Valor)
        {
            this.Valor = Valor.ToString();
            this.Coluna = Coluna;
        }
        public Celula(string Coluna, bool Valor)
        {
            this.Valor = Valor.ToString();
            this.Coluna = Coluna;
        }
        public Celula(string Coluna, object Valor)
        {
            this.Valor = Valor.ToString();
            this.Coluna = Coluna;
        }
        public Celula(string Coluna, DateTime Valor)
        {
            this.Valor = Valor.ToShortDateString();
            this.Coluna = Coluna;
        }
    }
    public class Valor
    {
        public string Serializar()
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            jsonString = JsonSerializer.Serialize(this, options);
            return jsonString;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is Valor)
            {
                if ((obj as Valor).valor == this.valor)
                {
                    return true;
                }
            }
            else if (obj is string)
            {
                return obj.ToString() == this.valor;
            }
            return false;
        }
        public override string ToString()
        {
            return valor;
        }
        public DateTime Data
        {
            get
            {
                return Utilz.Data(valor);
            }
        }
        public bool Boolean
        {
            get
            {
                return Utilz.Boolean(valor);
            }
        }

        public double Double(int Decimais = 4)
        {
            return Utilz.Double(valor, Decimais);
        }
        public int Int
        {
            get
            {
                string comps = valor;
                if (comps == "") { comps = "0"; }
                try
                {
                    return Convert.ToInt32(Math.Ceiling(Double()));
                }
                catch (Exception)
                {

                    return 0;
                }
            }
        }

        public string valor { get; private set; } = "";

        public Valor(string valor)
        {
            this.valor = valor;
        }

        public Valor()
        {

        }


    }
}
