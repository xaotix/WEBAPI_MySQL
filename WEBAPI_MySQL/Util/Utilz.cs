using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace daniel_api
{
    public class Utilz
    {
        public class Criptografia
        {
            private const int Keysize = 128;
                        private const int DerivationIterations = 1000;

            public static string Criptografar(string plainText, string chave)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(chave, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 128;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Descriptografar(string cipherText, string chave)
            {
                try
                {
                    var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                    var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                    var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                    var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                    using (var password = new Rfc2898DeriveBytes(chave, saltStringBytes, DerivationIterations))
                    {
                        var keyBytes = password.GetBytes(Keysize / 8);
                        using (var symmetricKey = new RijndaelManaged())
                        {
                            symmetricKey.BlockSize = 128;
                            symmetricKey.Mode = CipherMode.CBC;
                            symmetricKey.Padding = PaddingMode.PKCS7;
                            using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                            {
                                using (var memoryStream = new MemoryStream(cipherTextBytes))
                                {
                                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                    {
                                        var plainTextBytes = new byte[cipherTextBytes.Length];
                                        var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                        memoryStream.Close();
                                        cryptoStream.Close();
                                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                    return "";
                }

            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[16]; 
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }
        public static int GetId(string ma)
        {
            if(ma.Length>=6)
            {
                var db = new Banco();
                Registro rg = new Registro();
                rg.Tabela = Vars.TabelaUsers;
                rg.Filtros.Add(new Celula("ma", ma));
                rg.Banco = Vars.BancoUsers;
                var users = db.Consulta(rg,new Tabela());
                if(users.Linhas.Count>0)
                {
                    return users.Linhas[0].Get("id").Int();
                }
            }
            return -1;
        }

        public static Registro GetUser(string ma, string s)
        {
            if (ma.Length >= 6)
            {
                var db = new Banco();
                Registro rg = new Registro();
                rg.Tabela = Vars.TabelaUsers;
                rg.Filtros.Add(new Celula("ma", ma));
                rg.Banco = Vars.BancoUsers;
                var users = db.Consulta(rg, new Tabela());
                if (users.Linhas.Count > 0)
                {
                    var user = users.Linhas[0];
                    if(user.Get("s").ToString() == "")
                    {
                        if(s.Length<6)
                        {
                            user.Status = "Senha deve conter pelo menos 6 caracteres";
                        }
                        var senha = Utilz.Criptografia.Criptografar(s, "sdaqwer123");

                        user.Set("s", senha);
                        Registro pp = new Registro();
                        pp.Tabela = Vars.TabelaUsers;
                        pp.Banco = Vars.BancoUsers;
                        pp.Filtros.Add(new Celula("ma", ma));
                        pp.Valores.Add(new Celula("s",senha));
                        string mensagem;
                        db.Atualizar(pp, out mensagem);
                    }
                    return user;
                }
            }
            return new Registro() {  Status = "Usuário não encontrado"};
        }
        public static Tabela Logar(Tabela retorno)
        {

          
            

            try
            {
                var regexItem = new Regex(@"^\w+$");

                if (retorno.Status != "OK")
                {
                    return retorno;
                }

                if (retorno.ma == null | retorno.ma == "" | retorno.s == "" | retorno.s == null)
                {
                    retorno.Status = "Faltam dados de login.";
                    return retorno;
                }
                else if(!regexItem.IsMatch(retorno.ma))
                {
                    retorno.Status = "MA não pode conter caracteres especiais.";
                    return retorno;
                }
                else if (retorno.ma.Length < 6)
                {
                    retorno.Status = "MA deve conter pelo menos 6 caracteres.";
                    return retorno;
                }


                if (Banco.Tipo == Tipo_Conexao.SemActiveDirectory)
                {
                    retorno.Status = "OK";

                    var s = GetUser(retorno.ma, retorno.s);
                    
                    retorno.id_user = s.Get("id").Int();
                    retorno.Nome = s.Get("nome").ToString();
                    retorno.Email = s.Get("email").ToString();
                    var ss = s.Get("s").ToString();
                    var senha = Utilz.Criptografia.Descriptografar(ss,"sdaqwer123");
                    if (senha.ToUpper().Replace(" ","") == retorno.s.ToUpper().Replace(" ",""))
                    {
                        retorno.Status = "OK";
                    }
                    else
                    {
                        retorno.Status = "Senha Incorreta. Se tem certeza que sua senha está correta, solicite para resetar a senha para o administrador.";
                    }
                    if(retorno.id_user<=0)
                    {
                        retorno.Status = "Usuário não cadastrado.";
                    }
                    return retorno;
                }
                else
                {
                    System.DirectoryServices.DirectoryEntry directoryEntry = new System.DirectoryServices.DirectoryEntry("LDAP://medabil.com.br", retorno.ma, retorno.s);
                    DirectorySearcher searcher = new DirectorySearcher(directoryEntry);
                    searcher.PageSize = 1000;
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.Filter = "(&(samAccountType=805306368)(sAMAccountName=" + retorno.ma + "))";
                    // specify which property values to return in the search
                    searcher.PropertiesToLoad.Add("givenName");   // first name
                    searcher.PropertiesToLoad.Add("sn");          // last name
                    searcher.PropertiesToLoad.Add("mail");        // smtp mail address

                    SearchResult resultados = null;

                    resultados = searcher.FindOne();

                    if (resultados != null)
                    {
                        retorno.id_user = GetId(retorno.ma);

                        string nome = resultados.Properties["givenName"][0].ToString();
                        string sobrenome = resultados.Properties["sn"][0].ToString();
                        string email = resultados.Properties["mail"][0].ToString();

                        retorno.Nome = $"{nome} {sobrenome}";
                        retorno.Email = email;

                        if (retorno.id_user < 0)
                        {
                            Registro rg = new Registro();
                            rg.Tabela = Vars.TabelaUsers;
                            rg.Banco = Vars.BancoUsers;

                            rg.Valores.Add(new Celula("ma", retorno.ma));
                            rg.Valores.Add(new Celula("nome", $"{nome} {sobrenome}".ToUpper()));
                            rg.Valores.Add(new Celula("email", email));


                            var db = new Banco();
                            string msg;
                            retorno.id_user = (int)db.Cadastro(rg, out msg);
                            if (retorno.id_user > 0)
                            {
                                retorno.Mensagem = "Novo usuário cadastrado no sistema. Solicite para que seja categorizado.";
                            }
                            else
                            {
                                retorno.Status = "Erro ao tentar criar usuário " + msg;
                                return retorno;
                            }
                        }

                        retorno.Status = "OK";
                        return retorno;
                    }
                    else if (resultados == null)
                    {
                        retorno.Status = "Usuário não encontrado.";
                        return retorno;
                    }
                }



            }
            catch (Exception ex)
            {
                retorno.Status = "Erro ao tentar logar na rede Medabil: " + ex.Message;
                return retorno;
            }



            retorno.Status = "OK";
            return retorno;
        }
        public static string aspas { get; set; } = "\"";
        private static void ValidaPasta()
        {
            if (!Directory.Exists(@"C:\Temp\"))
            {
                Directory.CreateDirectory(@"C:\Temp\");
            }
        }
        public static string Diretorio(string Arquivo)
        {
            FileInfo x = new FileInfo(Arquivo);
            return (x.Directory.FullName + @"\").Replace(@"\\", @"\");
        }
        public static DateTime Data(string Data)
        {
            try
            {
                return Convert.ToDateTime(Data);
            }
            catch (Exception)
            {


            }
            return new DateTime(1, 1, 1);
        }
        private static System.Globalization.CultureInfo US { get; set; } = new System.Globalization.CultureInfo("en-US");
        private static System.Globalization.CultureInfo BR { get; set; } = new System.Globalization.CultureInfo("pt-BR");
        public static double Double(object comp, int Decimais = 4)
        {
            try
            {

                double val;
                if (double.TryParse(comp.ToString(), System.Globalization.NumberStyles.Float, BR, out val))
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

                else if (double.TryParse(comp.ToString(), System.Globalization.NumberStyles.Float, US, out val))
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
        public static int Int(object comp)
        {
            string comps = comp.ToString();
            if (comps == "") { comps = "0"; }
            try
            {
                return Convert.ToInt32(Math.Ceiling(Double(comps.Replace(".", ","))));
            }
            catch (Exception)
            {

                return 0;
            }

        }
        public static bool Boolean(object obj)
        {
            try
            {
                return Convert.ToBoolean(obj);
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
    public class Funcoes
    {
        public static List<List<Registro>> QuebraLista(List<Registro> Lista, int Tamanho = 1000)
        {
            var list = new List<List<Registro>>();

            for (int i = 0; i < Lista.Count; i += Tamanho)
            {
                list.Add(Lista.GetRange(i, Math.Min(Tamanho, Lista.Count - i)));
            }

            return list;
        }
    }
}
