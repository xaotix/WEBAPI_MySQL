using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace daniel_api.Controllers
{
    [Route("")]
    [ApiController]
    public class Controladores : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<string> Get()
        {
            return  "{comando inválido Testev2}";
        }

        [HttpPost]
        [Route("consultar")]
        public ActionResult<string> Consultar([FromBody] dynamic pp)
        {
            Registro filtro = new Registro(pp);
            Tabela tabela = new Tabela(filtro);

            tabela = Utilz.Logar(tabela);
            if (tabela.Status != "OK")
            {
                return tabela.GetJSON();
            }

            var db = new Banco();
            tabela = db.Consulta(filtro,tabela);

            return tabela.GetJSON();
        }

        [HttpPost]
        [Route("salvar")]
        public ActionResult<string> Salvar([FromBody] dynamic pp)
        {
            Registro filtro = new Registro(pp);
            Tabela tabela = new Tabela(filtro);

            string status;
            tabela = Utilz.Logar(tabela);
            if (tabela.Status != "OK")
            {
                return tabela.GetJSON();
            }


            var db = new Banco();
            var p = db.Cadastro(filtro, out status);
            if (p < 0)
            {
                tabela.Status = status;
            }
            else
            {
                tabela.Status = "OK";
            }

            return tabela.GetJSON();
        }

        [HttpPost]
        [Route("apagar")]
        public ActionResult<string> Apagar([FromBody] dynamic pp)
        {
            Registro filtro = new Registro(pp);
            Tabela tabela = new Tabela(filtro);
           
            string status;
            tabela = Utilz.Logar(tabela);
            if (tabela.Status != "OK")
            {
                return tabela.GetJSON();
            }

            var db = new Banco();
            var p = db.Apagar(filtro, out status);
            if (p ==false)
            {
                tabela.Status = status;
            }
            else
            {
                tabela.Status = "OK";
            }
            return tabela.GetJSON();
        }

        [HttpPost]
        [Route("atualizar")]
        public ActionResult<string> Atualizar([FromBody] dynamic pp)
        {
            Registro filtro = new Registro(pp);
            Tabela tabela = new Tabela(filtro);

            string status;
            tabela = Utilz.Logar(tabela);
            if (tabela.Status != "OK")
            {
                return tabela.GetJSON();
            }


            var db = new Banco();

            var p = db.Atualizar(filtro, out status);
            if (p == false)
            {
                tabela.Status = status;
            }
            else
            {
                tabela.Status = "OK";
            }
            return tabela.GetJSON();
        }

    }
}
