using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace daniel_api
{
    public class Banco
    {
        public static Tipo_Conexao Tipo { get; set; } = Tipo_Conexao.SemActiveDirectory;
        public string Servidor { get; set; } = Vars.Servidor;
        public string Usuario { get; set; } = "";
        public string Senha { get; set; } = "";
        public string BancoDeDados
        {
            get
            {
                if(Tipo == Tipo_Conexao.SemActiveDirectory)
                {
                    return _Dbase;
                }
                return _DbaseActiveDirectory;
            }
            set
            {
                _DbaseActiveDirectory = value;
            }
        }

        public string _Dbase { get; set; } = Vars.BancoUsers;
        private string _DbaseActiveDirectory { get; set; } = Vars.BancoActiveDirectory;
        public string Porta { get; set; } = Vars.Porta;

        public bool GetEstaOnline()
        {
            if (Conexao == null) { return false; }
            try
            {
                if (Conexao.State == ConnectionState.Open)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;

            }

            return false;
        }

        public bool Conectar()
        {
            if (Servidor == "" | Porta == "" | Usuario == "")
            {
                return false;
            }
            if (GetEstaOnline() == false)
            {
                //Definição do dataset
                bdDataSet = new DataSet();
                //Define string de conexão
                Conexao = new MySqlConnection("server=" + Servidor + ";Port=" + Porta + ";" + "user id=" + Usuario + ";password=" + Senha + ";database=" + BancoDeDados + "; convert zero datetime=True");

                try
                {
                    Conexao.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return GetEstaOnline();

        }
        public void Desconectar()
        {
            if (Conexao != null)
            {
                //if (Conexao.State == ConnectionState.Open)
                //{
                //    Conexao.Dispose();
                //    Conexao.Close();
                //}

                Conexao.Dispose();
                Conexao.Close();
            }

        }
        private MySqlCommand ExecutarComando(string Comando, DataTable Tab = null)
        {
            MySqlCommand Ex = new MySqlCommand();

            if (Tab == null)
            {
                Tab = new DataTable();

            }

            Conectar();
            Ex.Connection = Conexao;
            Ex.CommandText = Comando;
            Ex.CommandType = CommandType.Text;
            MinhasExecucoes = new MySqlDataAdapter();
            MinhasExecucoes.SelectCommand = Ex;
            //Ex.ExecuteNonQuery();
            MinhasExecucoes.Fill(Tab);
            MinhasExecucoes.Dispose();

            return Ex;



        }
        public List<string> RetornarColunas(string Database, string Tabela, out string mensagem)
        {

            var colunas = new List<String>();
            var instrucaoSQL = "select column_name from information_schema.columns where table_name='" + Tabela + "' and table_schema = '" + Database + "'";
            try
            {
                TableBuffer = new DataTable();
                Conectar();
                Conexao.ChangeDatabase(Database);
                MySqlCommand ex = ExecutarComando(instrucaoSQL, TableBuffer);
                var sqlDataReader = ex.ExecuteReader();
                if (sqlDataReader.HasRows)
                {
                    while (sqlDataReader.Read())
                    {
                        colunas.Add(sqlDataReader[0].ToString());
                    }
                    sqlDataReader.Close();
                    sqlDataReader.Dispose();
                }
            }

            catch (Exception ex)
            {
                mensagem = ex.Message + "\n" + ex.StackTrace;
            }

            Desconectar();
            mensagem = "";
            return colunas;

        }
        public Banco()
        {
            if(Tipo == Tipo_Conexao.ActiveDirectory)
            {
                this.Servidor = Vars.Servidor;
                this.Porta = "";
                this.Usuario = "";
                this.Senha = "";
                this.BancoDeDados = "";
            }
            else if(Tipo == Tipo_Conexao.SemActiveDirectory)
            {
                this.Servidor = Vars.Servidor2;
                this.Porta = "";
                this.Usuario = "";
                this.Senha = "";
            }

        }

        private MySqlConnection Conexao { get; set; }
        private MySqlDataAdapter MinhasExecucoes { get; set; } = new MySqlDataAdapter();
        private DataSet bdDataSet { get; set; }
        private DataTable TableBuffer { get; set; } = new DataTable();

        public Tabela Consulta(string Comando)
        {
            Tabela Retorno = new Tabela();
            try
            {
                MySqlCommand Ex = ExecutarComando(Comando, TableBuffer);
                var sqlDataReader = Ex.ExecuteReader();
                if (sqlDataReader.HasRows)
                {
                    List<string> Colunas = new List<string>();
                    for (int i = 0; i < sqlDataReader.FieldCount; i++)
                    {
                        Colunas.Add(sqlDataReader.GetName(i));
                    }
                    while (sqlDataReader.Read())
                    {
                        Registro nl = new Registro();
                        foreach (string Coluna in Colunas)
                        {
                            Celula n = new Celula(Coluna, sqlDataReader[Coluna].ToString());
                            nl.Valores.Add(n);
                        }
                        Retorno.Linhas.Add(nl);
                    }
                    Retorno.Resultados = Retorno.Linhas.Count();
                    Retorno.Status = "OK";
                    sqlDataReader.Close();
                    sqlDataReader.Dispose();
                }
            }
            catch (Exception ex)
            {
                Retorno.Status = ex.Message + "\n" + ex.StackTrace;
            }
            Desconectar();

            return Retorno;
        }
        public long Verificar(Registro Criterios, List<string> Colunas, out string mensagem)
        {
            var regexItem = new Regex(@"^\w+$");

            if (Criterios.Banco == "" | Criterios.Banco == null)
            {
                mensagem = "Campo Banco em branco.";
                return -1;
            }
            if (Criterios.Tabela == "" | Criterios.Tabela == null)
            {
                mensagem = "Campo tabela em branco.";
                return -1;
            }
            if(Criterios.Valores == null)
            {
                mensagem = "Nenhuma coluna setada para o registro.";
                return -1;
            }

            if (Criterios.Valores.Count == 0 && Criterios.Filtros.Count==0)
            {
                mensagem = "Nenhuma coluna setada para o registro.";
                return -1;
            }

            if (!regexItem.IsMatch(Criterios.Banco))
            {
                mensagem = "Campo Banco contém caracteres especiais inválidos.";
                return -1;
            }


            //evitar comandos maliciosos
            if (!regexItem.IsMatch(Criterios.Tabela))
            {
                mensagem = "Campo Tabela contém caracteres especiais inválidos.";
                return -1;
            }

            //evitar comandos maliciosos
            var colunas_nao_oq = Criterios.Valores.Select(x => x.Coluna).Distinct().ToList().FindAll(x => !regexItem.IsMatch(x) | x==null);
            if (colunas_nao_oq.Count > 0)
            {
                mensagem = "As colunas a seguir contém caracteres especiais: " + string.Join(", ", colunas_nao_oq);
                return -1;
            }


            var colunas_repetidas = Criterios.Valores.GroupBy(x => x.Coluna.ToUpper()).ToList().FindAll(x => x.Count() > 1);

            if (colunas_repetidas.Count > 0)
            {
                mensagem = "As colunas a seguir aparecem mais de uma vez: " + string.Join(", ", colunas_repetidas.Select(x => x.Key));
                return -1;
            }

            if (Criterios.Valores.FindAll(x => x.Coluna.Replace(" ", "") == "").Count > 0)
            {
                mensagem = "Há colunas sem nome definido.";
                return -1;
            }


            if (Criterios.Filtros.FindAll(x => x.Coluna.Replace(" ", "") == "").Count > 0)
            {
                mensagem = "Há colunas sem nome definido no filtro.";
                return -1;
            }
            if (Colunas.Count > 0)
            {
                var colunas_que_nao_existesm = Criterios.Valores.FindAll(x => Colunas.Find(y => y.ToUpper() == x.Coluna.ToUpper()) == null);
                var colunas_que_nao_existesm_filtro = Criterios.Filtros.FindAll(x => Colunas.Find(y => y.ToUpper() == x.Coluna.ToUpper()) == null);

                if (colunas_que_nao_existesm.Count > 0)
                {
                    mensagem = "As colunas a seguir não existem na tabela: " + Criterios.Tabela + ":" + string.Join(", ", colunas_que_nao_existesm.Select(x => x.Coluna));
                    return -1;
                }
                if (colunas_que_nao_existesm_filtro.Count > 0)
                {
                    mensagem = "As colunas a seguir não existem na tabela: " + Criterios.Tabela + ":" + string.Join(", ", colunas_que_nao_existesm_filtro.Select(x => x.Coluna));
                    return -1;
                }
            }
            else if(Colunas.Count==0)
            {
                mensagem = $"A tabela {Criterios.Tabela} ou a dbase {Criterios.Banco} não foram encontradas. (não há nenhuma coluna a listar)";
                return -1;
            }

            mensagem = "";
            return 0;
        }
        public long Cadastro(Registro Criterios, out string mensagem)
        {
            var Valores = Criterios.Valores;
            if(Valores.Count==0)
            {
                mensagem = "Não há nenhum item na lista";
                return -1;
            }

            List<string> Colunas =  RetornarColunas(Criterios.Banco, Criterios.Tabela, out mensagem);
            if(mensagem!="")
            {
                return -1;
            }
            var ss = Verificar(Criterios, Colunas, out mensagem);
            if(ss<0)
            {
                return ss;
            }
            

            try
            {
                string Comando = Comando = "INSERT INTO " + Criterios.Banco + "." + Criterios.Tabela + " (";
                Valores = Valores.FindAll(x => Colunas.Find(y => y == x.Coluna) != null);
                string Columns = "";
                string Vals = "";
                for (int i = 0; i < Valores.Count(); i++)
                {
                    Columns = Columns + "`" + Valores[i].Coluna + "`";
                    Vals = Vals + "'" + MySql.Data.MySqlClient.MySqlHelper.EscapeString(Valores[i].Valor.Replace(",", ".")) + "'";
                    if (i < Valores.Count - 1)
                    {
                        Columns = Columns + ",";
                        Vals = Vals + ",";
                    }
                }
                Comando = Comando + Columns + ") values (" + Vals + ")";
                MySqlCommand cc = ExecutarComando(Comando, TableBuffer);
                Desconectar();
                mensagem = "OK";
                return cc.LastInsertedId;
            }
            catch (Exception ex)
            {
                mensagem = ex.Message + "\n" + ex.StackTrace;
                return -1;
            }
        }
        public Tabela Consulta(Registro Criterios,Tabela retorno)
        {
            string condicional = "And";
            List<Registro> registros = new List<Registro>();

            if(Criterios.Filtros.Count==0 && Criterios.Valores.Count>0)
            {
                Criterios.Filtros.AddRange(Criterios.Valores);
                Criterios.Valores.Clear();
            }
            string mensagem = "";

            List<string> Colunas = RetornarColunas(Criterios.Banco, Criterios.Tabela, out mensagem);

            if (mensagem != "")
            {
                retorno.Status = mensagem;
                return  retorno;
            }

            if (Criterios.Filtros.Count == 0)
            {
                mensagem = "Não há nenhum item na lista de Filtro";
                retorno.Status = mensagem;
                return retorno;
            }
            var s = Verificar(Criterios, Colunas, out mensagem);
            if (s<0)
            {
                retorno.Status = mensagem;
                return retorno;
            }
           

            string Comando = "select * from " + Criterios.Banco + "." + Criterios.Tabela;

            if(Criterios.Filtros.Count>0)
            {
                Comando = Comando + " where ";
                for (int i = 0; i < Criterios.Filtros.Count; i++)
                {
                    Comando = Comando + "`" + Criterios.Filtros[i].Coluna + (Criterios.Filtros[i].Valor.Contains("%") ? "` like '" : "` = '") + Criterios.Filtros[i].Valor + "'";
                    if (i < Criterios.Filtros.Count - 1)
                    {
                        Comando = Comando + " " + condicional + " ";
                    }
                }
            }
           
            
            try
            {
                MySqlCommand Ex = ExecutarComando(Comando, TableBuffer);

                var sqlDataReader = Ex.ExecuteReader();

                if (sqlDataReader.HasRows)

                {
                    while (sqlDataReader.Read())
                    {
                        Registro nl = new Registro(Criterios.Tabela, Colunas, sqlDataReader);
                        registros.Add(nl);
                    }
                   
                    sqlDataReader.Close();
                    sqlDataReader.Dispose();
                    Desconectar();

                }
            }
            catch (Exception ex)
            {
                retorno.Status = "Erro ao executar comando: " + ex.Message + "\n" + ex.StackTrace;
                return retorno;
            }
            retorno.Status = "OK";
            retorno.Linhas = registros;
            return retorno;
        }
        public bool Apagar(Registro Criterios, out string mensagem)
        {
            try
            {
                if(Criterios.Filtros.Count==0 && Criterios.Valores.Count>0)
                {
                    Criterios.Filtros.AddRange(Criterios.Filtros);
                    Criterios.Valores.Clear();
                }
                if(Criterios.Filtros.Count==0)
                {
                    mensagem = "Não há nenhum critério adicionado.";
                    return false;
                }
                string condicional = "And";
                List<string> Colunas = RetornarColunas(Criterios.Banco, Criterios.Tabela, out mensagem);
                if(mensagem!="")
                {
                    return false;
                }
                var s = Verificar(Criterios, Colunas, out mensagem);
                if (s < 0)
                {
                    return false;
                }
                string chaveComando = "";
                for (int i = 0; i < Criterios.Filtros.Count; i++)
                {
                    chaveComando = chaveComando + "`" + Criterios.Filtros[i].Coluna + (Criterios.Filtros[i].Valor.Contains("%")?"` like '":"` = '") + MySql.Data.MySqlClient.MySqlHelper.EscapeString(Criterios.Filtros[i].Valor) + "'";
                    if (i < Criterios.Filtros.Count - 1)
                    {
                        chaveComando = chaveComando + " " + condicional + " ";
                    }
                }

                string ComandoFIM = "";

                ComandoFIM = "DELETE FROM " + Criterios.Banco + "." + Criterios.Tabela + " Where " + chaveComando;
                ExecutarComando(ComandoFIM);
                Desconectar();
                mensagem ="OK";
                return true;
            }
            catch (Exception ex)
            {
                mensagem = ex.Message + "\n" + ex.StackTrace;
                return false;
            }

        }
        public bool Atualizar(Registro Criterios, out string mensagem)
        {
            try
            {
                if (Criterios.Filtros.Count == 0)
                {
                    mensagem = "Não há nenhum critério adicionado.";
                    return false;
                }
                List<string> Colunas = RetornarColunas(Criterios.Banco, Criterios.Tabela, out mensagem);
                if(mensagem!="")
                {
                    return false;
                }
                var s = Verificar(Criterios, Colunas, out mensagem);
                if(s<0)
                {
                    return false;
                }
                string Comando = "";
                string filtro = "";
                for (int i = 0; i < Criterios.Valores.Count; i++)
                {
                    Comando = Comando + "`" + Criterios.Valores[i].Coluna + "` = '" + MySql.Data.MySqlClient.MySqlHelper.EscapeString(Criterios.Valores[i].Valor) + "'";


                    if (i < Criterios.Valores.Count - 1)
                    {
                        Comando = Comando + " , ";
                    }
                }

                for (int i = 0; i < Criterios.Filtros.Count; i++)
                {
                    filtro = filtro + "`" + Criterios.Filtros[i].Coluna + (Criterios.Filtros[i].Valor.Contains("%") ? "` like '" : "` = '") + MySql.Data.MySqlClient.MySqlHelper.EscapeString(Criterios.Filtros[i].Valor) + "'";
                    if (i < Criterios.Filtros.Count - 1)
                    {
                        filtro = filtro + " AND ";
                    }
                }
                string ComandoFIM = "";

                ComandoFIM = "UPDATE " + Criterios.Banco + "." + Criterios.Tabela + " SET " + Comando + " Where " + filtro;
                ExecutarComando(ComandoFIM);

                Desconectar();
                mensagem = "OK";
                return true;
            }
            catch (Exception ex)
            {
                mensagem = ex.Message + "\n" + ex.StackTrace;
                return false;
            }
        }
    }
}
