using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransacaoRenave.Entities;

namespace TransacoesRotina
{
    public class Transacao
    {
        private readonly string diretorioLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");

        string idTransacao;
        EstoqueJson estoque;

        string caminhoFisicoPdfAtpv;
        string caminhoLogicoPdfAtpv;

        public Transacao()
        {
            try
            {
                IEnumerable<TransacaoPendenteEnvio> envio = ConsultaVeiculosPendentesDeEnvio();

                if (envio != null)
                {
                    foreach (TransacaoPendenteEnvio item in envio)
                    {
                        switch (item.tipoTransacao)
                        {
                            case "saidas-estoque-veiculo-zero-km-renovar":
                                estoque = DespachanteSaidaEstoqueZeroKmRenovar(item, out idTransacao);
                                SalvarDadosServidorSolicitacaoSaidaEstoqueZeroKmRenovar(estoque, item, idTransacao);
                                WsPdfAtpv(item);
								RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira.ConfirmarUtilizacao, 2, item);
								break;
                            case "saidas-estoque-veiculo-inacabado-renovar":
                                estoque = DespachanteSaidaEstoqueInacabadoRenovar(item, out idTransacao);
                                SalvarDadosServidorSolicitacaoSaidaEstoqueInacabadoRenovar(estoque, item, idTransacao);
								RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira.ConfirmarUtilizacao, 2, item);
								break;
                            case "ite/saidas-estoque-renovar":
                                estoque = DespachanteSaidaIteRenovar(item, out idTransacao);
                                SalvarDadosServidorSolicitacaoSaidaIteRenovar(estoque, item, idTransacao);
                                WsPdfAtpvIte(item);
								RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira.ConfirmarUtilizacao, 2, item);
								break;
                            case "montadora/saidas-estoque-veiculo-zero-km-renovar":
                                estoque = DespachanteSaidaEstoqueZeroKmRenovarPelaMontadora(item, out idTransacao);
                                SalvarDadosServidorSolicitacaoSaidaEstoqueZeroKmRenovarPelaMontadora(estoque, item, idTransacao);
                                WsPdfAtpvMontadora(item);
								RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira.ConfirmarUtilizacao, 2, item);
								break;
                            case "montadora/saidas-estoque-veiculo-inacabado-renovar":
                                estoque = DespachanteSaidaEstoqueInacabadoRenovarPelaMontadora(item, out idTransacao);
                                SalvarDadosServidorSolicitacaoSaidaEstoqueInacabadoRenovarPelaMontadora(estoque, item, idTransacao);
								RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira.ConfirmarUtilizacao, 2, item);
								break;
                        }

                        Log("Saídas Renovar realizadas com sucesso");
                    }
                }

                AtualizarMonitor();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private EstoqueJson DespachanteSaidaEstoqueZeroKmRenovar(TransacaoPendenteEnvio envio, out string idTransacao)
        {
			SolicitacaoSaidaEstoqueZeroKmRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueZeroKmRenovar>(envio.jsonEnvio);

			ComunicadorRenaveRequest request = new ComunicadorRenaveRequest()
            {
                Token = Util.GetToken(),
                IdEstabelecimento = envio.idEstabelecimento,
                CodigoTransacao = "saidas-estoque-veiculo-zero-km-renovar",
                Url = $"{Util.BaseAddressWsRenave()}/saidas-estoque-veiculo-zero-km-renovar",
                MetodoRequisicao = "POST",
                IdTransacao = "",
                JsonEnvio = JsonConvert.SerializeObject(solicitacao),
                JsonRetorno = "",
                IdProcesso = envio.idProcesso,
                IdSessao = "-1"
            };

            string jsonRetorno = new DespachanteRenave().Send(request, out idTransacao);

            if (string.IsNullOrEmpty(jsonRetorno))
            {
                throw new InvalidOperationException("Verifique se o aplicativo está conectado. Foi realizada uma tentativa de envio da transação, mas o retorno não está compatível com o resultado esperado.");
            }

            try
            {
                return JsonConvert.DeserializeObject<EstoqueJson>(jsonRetorno);
            }
            catch
            {
                throw new InvalidOperationException(jsonRetorno);
            }
        }

        private EstoqueJson DespachanteSaidaEstoqueZeroKmRenovarPelaMontadora(TransacaoPendenteEnvio envio, out string idTransacao)
        {
			SolicitacaoSaidaEstoqueZeroKmPelaMontadoraRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueZeroKmPelaMontadoraRenovar>(envio.jsonEnvio);

			ComunicadorRenaveRequest request = new ComunicadorRenaveRequest()
            {
                Token = Util.GetToken(),
				IdEstabelecimento = envio.idEstabelecimento,
                CodigoTransacao = "montadora/saidas-estoque-veiculo-zero-km-renovar",
                Url = $"{Util.BaseAddressWsRenave()}/montadora/saidas-estoque-veiculo-zero-km-renovar",
                MetodoRequisicao = "POST",
                IdTransacao = "",
                JsonEnvio = JsonConvert.SerializeObject(solicitacao),
                JsonRetorno = "",
                IdProcesso = envio.idProcesso,
                IdSessao = "-1"
            };

            string jsonRetorno = new DespachanteRenave().Send(request, out idTransacao);

            if (string.IsNullOrEmpty(jsonRetorno))
            {
                throw new InvalidOperationException("Verifique se o aplicativo está conectado. Foi realizada uma tentativa de envio da transação, mas o retorno não está compatível com o resultado esperado.");
            }

            try
            {
                return JsonConvert.DeserializeObject<EstoqueJson>(jsonRetorno);
            }
            catch
            {
                throw new InvalidOperationException(jsonRetorno);
            }
        }

        private EstoqueJson DespachanteSaidaEstoqueInacabadoRenovar(TransacaoPendenteEnvio envio, out string idTransacao)
        {
			SolicitacaoSaidaEstoqueInacabadoRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueInacabadoRenovar>(envio.jsonEnvio);

			ComunicadorRenaveRequest request = new ComunicadorRenaveRequest()
            {
                Token = Util.GetToken(),
                IdEstabelecimento = envio.idEstabelecimento,
                CodigoTransacao = "saidas-estoque-veiculo-inacabado-renovar",
                Url = $"{Util.BaseAddressWsRenave()}/saidas-estoque-veiculo-inacabado-renovar",
                MetodoRequisicao = "POST",
                IdTransacao = "",
                JsonEnvio = JsonConvert.SerializeObject(solicitacao),
                JsonRetorno = "",
                IdProcesso = envio.idProcesso,
                IdSessao = "-1"
            };

            string jsonRetorno = new DespachanteRenave().Send(request, out idTransacao);

            if (string.IsNullOrEmpty(jsonRetorno))
            {
                throw new InvalidOperationException("Verifique se o aplicativo está conectado. Foi realizada uma tentativa de envio da transação, mas o retorno não está compatível com o resultado esperado.");
            }

            try
            {
                return JsonConvert.DeserializeObject<EstoqueJson>(jsonRetorno);
            }
            catch
            {
                throw new InvalidOperationException(jsonRetorno);
            }
        }

        private EstoqueJson DespachanteSaidaEstoqueInacabadoRenovarPelaMontadora(TransacaoPendenteEnvio envio, out string idTransacao)
        {
			SolicitacaoSaidaEstoqueInacabadoPelaMontadoraRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueInacabadoPelaMontadoraRenovar>(envio.jsonEnvio);

			ComunicadorRenaveRequest request = new ComunicadorRenaveRequest()
            {
                Token = Util.GetToken(),
                IdEstabelecimento = envio.idEstabelecimento,
                CodigoTransacao = "montadora/saidas-estoque-veiculo-inacabado-renovar",
                Url = $"{Util.BaseAddressWsRenave()}/montadora/saidas-estoque-veiculo-inacabado-renovar",
                MetodoRequisicao = "POST",
                IdTransacao = "",
                JsonEnvio = JsonConvert.SerializeObject(solicitacao),
                JsonRetorno = "",
                IdProcesso = envio.idProcesso,
                IdSessao = "-1"
            };

            string jsonRetorno = new DespachanteRenave().Send(request, out idTransacao);

            if (string.IsNullOrEmpty(jsonRetorno))
            {
                throw new InvalidOperationException("Verifique se o aplicativo está conectado. Foi realizada uma tentativa de envio da transação, mas o retorno não está compatível com o resultado esperado.");
            }

            try
            {
                return JsonConvert.DeserializeObject<EstoqueJson>(jsonRetorno);
            }
            catch
            {
                throw new InvalidOperationException(jsonRetorno);
            }
        }


        private EstoqueJson DespachanteSaidaIteRenovar(TransacaoPendenteEnvio envio, out string idTransacao)
        {
			SolicitacaoSaidaEstoqueRenovarIte solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueRenovarIte>(envio.jsonEnvio);

			ComunicadorRenaveRequest request = new ComunicadorRenaveRequest()
            {
                Token = Util.GetToken(),
                IdEstabelecimento = envio.idEstabelecimento,
                CodigoTransacao = "ite/saidas-estoque-renovar",
                Url = $"{Util.BaseAddressWsRenave()}/ite/saidas-estoque-renovar",
                MetodoRequisicao = "POST",
                IdTransacao = "",
                JsonEnvio = JsonConvert.SerializeObject(solicitacao),
                JsonRetorno = "",
                IdProcesso = envio.idProcesso,
                IdSessao = "-1"
            };

            string jsonRetorno = new DespachanteRenave().Send(request, out idTransacao);

            if (string.IsNullOrEmpty(jsonRetorno))
            {
                throw new InvalidOperationException("Verifique se o aplicativo está conectado. Foi realizada uma tentativa de envio da transação, mas o retorno não está compatível com o resultado esperado.");
            }

            try
            {
                return JsonConvert.DeserializeObject<EstoqueJson>(jsonRetorno);
            }
            catch
            {
                throw new InvalidOperationException(jsonRetorno);
            }
        }

        private void SalvarDadosServidorSolicitacaoSaidaEstoqueZeroKmRenovar(EstoqueJson estoque, TransacaoPendenteEnvio envio, string idTransacao)
        {
			SolicitacaoSaidaEstoqueZeroKmRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueZeroKmRenovar>(envio.jsonEnvio);

			Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idSessao", "-1");
            parametros.Add("@idProcesso", envio.idProcesso);
            parametros.Add("@EmailComprador", estoque.saidaEstoque.comprador.email);
            //parametros.Add("@EmailEstabelecimento", estoque.saidaEstoque.comprado);
            //parametros.Add("@ValorVenda", estoque.valor.ToString());
            parametros.Add("@idTransacao", idTransacao);
            parametros.Add("@EstabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar", solicitacao.estabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar);
            parametros.Add("@tipoSolicitacaoSaida", 88);

            if (estoque != null)
            {
                parametros.Add("@Estado", estoque.estado);

                if (estoque.saidaEstoque != null)
                {
                    parametros.Add("@ChaveNotaFiscalSaida", estoque.saidaEstoque.chaveNotaFiscalSaida);
                    parametros.Add("@CpfOperadorResponsavelSaidaEstoque", estoque.saidaEstoque.cpfOperadorResponsavel);
                    parametros.Add("@DataVenda", Util.FormatarData(estoque.saidaEstoque.dataHora, 120));
                    parametros.Add("@DataEnvioNotaFiscalSaida", Util.FormatarData(estoque.saidaEstoque.dataHoraEnvioNotaFiscalSaida, 120));

                    if (estoque.saidaEstoque.comprador != null)
                    {
                        parametros.Add("@TipoDocumentoComprador", estoque.saidaEstoque.comprador.tipoDocumento);
                        parametros.Add("@DocumentoComprador", estoque.saidaEstoque.comprador.numeroDocumento);
                        parametros.Add("@NomeComprador", estoque.saidaEstoque.comprador.nome);

                        if (estoque.saidaEstoque.comprador.endereco != null)
                        {
                            parametros.Add("@Bairro", estoque.saidaEstoque.comprador.endereco.bairro);
                            parametros.Add("@CEP", estoque.saidaEstoque.comprador.endereco.cep);
                            parametros.Add("@Complemento", estoque.saidaEstoque.comprador.endereco.complemento);
                            parametros.Add("@Logradouro", estoque.saidaEstoque.comprador.endereco.logradouro);
                            parametros.Add("@Numero", estoque.saidaEstoque.comprador.endereco.numero);

                            if (estoque.saidaEstoque.comprador.endereco.municipio != null)
                            {
                                parametros.Add("@CodigoMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.id);
                                parametros.Add("@NomeMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.nome);
                                parametros.Add("@UFMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.uf);
                            }
                        }
                    }
                }
            }

            new Dao().ExecutarProcedure("stp_Est_SaidaZeroKmRenovar_Upd", parametros);
        }

        private void SalvarDadosServidorSolicitacaoSaidaEstoqueZeroKmRenovarPelaMontadora(EstoqueJson estoque, TransacaoPendenteEnvio envio, string idTransacao)
        {
			SolicitacaoSaidaEstoqueZeroKmPelaMontadoraRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueZeroKmPelaMontadoraRenovar>(envio.jsonEnvio);

			Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idSessao", "-1");
            parametros.Add("@idProcesso", envio.idProcesso);
            parametros.Add("@EmailComprador", estoque.saidaEstoque.comprador.email);
            //parametros.Add("@EmailEstabelecimento", _request.EmailEstabelecimento);
            //parametros.Add("@ValorVenda", _request.ValorVenda.ToString());
            parametros.Add("@idTransacao", idTransacao);
			parametros.Add("@EstabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar", solicitacao.estabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar);			
			parametros.Add("@CnpjConcessionariaVendedora", solicitacao.entregaIndicada.cnpjConcessionariaVendedora);
            parametros.Add("@CnpjConcessionariaEntregadora", solicitacao.entregaIndicada.cnpjConcessionariaEntregadora);
            parametros.Add("@TipoBeneficioTributario", solicitacao.tipoBeneficioTributario);
            parametros.Add("@tipoSolicitacaoSaida", 86);

            if (estoque != null)
            {
                parametros.Add("@Estado", estoque.estado);

                if (estoque.saidaEstoque != null)
                {
                    parametros.Add("@ChaveNotaFiscalSaida", estoque.saidaEstoque.chaveNotaFiscalSaida);
                    parametros.Add("@CpfOperadorResponsavelSaidaEstoque", estoque.saidaEstoque.cpfOperadorResponsavel);
                    parametros.Add("@DataVenda", Util.FormatarData(estoque.saidaEstoque.dataHora, 120));
                    parametros.Add("@DataEnvioNotaFiscalSaida", Util.FormatarData(estoque.saidaEstoque.dataHoraEnvioNotaFiscalSaida, 120));

                    if (estoque.saidaEstoque.comprador != null)
                    {
                        parametros.Add("@TipoDocumentoComprador", estoque.saidaEstoque.comprador.tipoDocumento);
                        parametros.Add("@DocumentoComprador", estoque.saidaEstoque.comprador.numeroDocumento);
                        parametros.Add("@NomeComprador", estoque.saidaEstoque.comprador.nome);

                        if (estoque.saidaEstoque.comprador.endereco != null)
                        {
                            parametros.Add("@Bairro", estoque.saidaEstoque.comprador.endereco.bairro);
                            parametros.Add("@CEP", estoque.saidaEstoque.comprador.endereco.cep);
                            parametros.Add("@Complemento", estoque.saidaEstoque.comprador.endereco.complemento);
                            parametros.Add("@Logradouro", estoque.saidaEstoque.comprador.endereco.logradouro);
                            parametros.Add("@Numero", estoque.saidaEstoque.comprador.endereco.numero);

                            if (estoque.saidaEstoque.comprador.endereco.municipio != null)
                            {
                                parametros.Add("@CodigoMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.id);
                                parametros.Add("@NomeMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.nome);
                                parametros.Add("@UFMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.uf);
                            }
                        }
                    }
                }
            }

            new Dao().ExecutarProcedure("stp_Est_SaidaZeroKmRenovarPelaMontadora_Upd", parametros);
        }

        private void SalvarDadosServidorSolicitacaoSaidaEstoqueInacabadoRenovar(EstoqueJson estoque, TransacaoPendenteEnvio envio, string idTransacao)
        {
			SolicitacaoSaidaEstoqueInacabadoRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueInacabadoRenovar>(envio.jsonEnvio);

			Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idSessao", "-1");
            parametros.Add("@idProcesso", envio.idProcesso);
            //parametros.Add("@EmailComprador", _request.Comprador.email);
            //parametros.Add("@EmailEstabelecimento", _request.EmailEstabelecimento);
            //parametros.Add("@ValorVenda", _request.ValorVenda.ToString());
            parametros.Add("@idTransacao", idTransacao);
            parametros.Add("@EstabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar", solicitacao.estabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar);
            parametros.Add("@leasingVeiculoInacabado", solicitacao.leasingVeiculoInacabado);
            parametros.Add("@tipoSolicitacaoSaida", 87);


            if (estoque != null)
            {
                parametros.Add("@Estado", estoque.estado);

                if (estoque.saidaEstoque != null)
                {
                    parametros.Add("@ChaveNotaFiscalSaida", estoque.saidaEstoque.chaveNotaFiscalSaida);
                    parametros.Add("@CpfOperadorResponsavelSaidaEstoque", estoque.saidaEstoque.cpfOperadorResponsavel);
                    parametros.Add("@DataVenda", Util.FormatarData(estoque.saidaEstoque.dataHora, 120));
                    parametros.Add("@DataEnvioNotaFiscalSaida", Util.FormatarData(estoque.saidaEstoque.dataHoraEnvioNotaFiscalSaida, 120));

                    if (estoque.saidaEstoque.comprador != null)
                    {
                        parametros.Add("@TipoDocumentoComprador", estoque.saidaEstoque.comprador.tipoDocumento);
                        parametros.Add("@DocumentoComprador", estoque.saidaEstoque.comprador.numeroDocumento);
                        parametros.Add("@NomeComprador", estoque.saidaEstoque.comprador.nome);

                        if (estoque.saidaEstoque.comprador.endereco != null)
                        {
                            parametros.Add("@Bairro", estoque.saidaEstoque.comprador.endereco.bairro);
                            parametros.Add("@CEP", estoque.saidaEstoque.comprador.endereco.cep);
                            parametros.Add("@Complemento", estoque.saidaEstoque.comprador.endereco.complemento);
                            parametros.Add("@Logradouro", estoque.saidaEstoque.comprador.endereco.logradouro);
                            parametros.Add("@Numero", estoque.saidaEstoque.comprador.endereco.numero);

                            if (estoque.saidaEstoque.comprador.endereco.municipio != null)
                            {
                                parametros.Add("@CodigoMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.id);
                                parametros.Add("@NomeMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.nome);
                                parametros.Add("@UFMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.uf);
                            }
                        }
                    }
                }
            }

            new Dao().ExecutarProcedure("stp_Est_SaidaInacabadoRenovar_Upd", parametros);
        }

        private void SalvarDadosServidorSolicitacaoSaidaEstoqueInacabadoRenovarPelaMontadora(EstoqueJson estoque, TransacaoPendenteEnvio envio, string idTransacao)
        {
			SolicitacaoSaidaEstoqueInacabadoPelaMontadoraRenovar solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueInacabadoPelaMontadoraRenovar>(envio.jsonEnvio);

			Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idSessao", "-1");
            parametros.Add("@idProcesso", envio.idProcesso);
            //parametros.Add("@EmailComprador", _request.Comprador.email);
            //parametros.Add("@EmailEstabelecimento", _request.EmailEstabelecimento);
            //parametros.Add("@ValorVenda", _request.ValorVenda.ToString());
            parametros.Add("@idTransacao", idTransacao);
			parametros.Add("@EstabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar", solicitacao.estabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar);
			parametros.Add("@leasingVeiculoInacabado", solicitacao.leasingVeiculoInacabado);
			parametros.Add("@tipoSolicitacaoSaida", 85);


            if (estoque != null)
            {
                parametros.Add("@Estado", estoque.estado);                

                if (estoque.saidaEstoque != null)
                {
                    parametros.Add("@ChaveNotaFiscalSaida", estoque.saidaEstoque.chaveNotaFiscalSaida);
                    parametros.Add("@CpfOperadorResponsavelSaidaEstoque", estoque.saidaEstoque.cpfOperadorResponsavel);
                    parametros.Add("@DataVenda", Util.FormatarData(estoque.saidaEstoque.dataHora, 120));
                    parametros.Add("@DataEnvioNotaFiscalSaida", Util.FormatarData(estoque.saidaEstoque.dataHoraEnvioNotaFiscalSaida, 120));

                    if (estoque.saidaEstoque.comprador != null)
                    {
                        parametros.Add("@TipoDocumentoComprador", estoque.saidaEstoque.comprador.tipoDocumento);
                        parametros.Add("@DocumentoComprador", estoque.saidaEstoque.comprador.numeroDocumento);
                        parametros.Add("@NomeComprador", estoque.saidaEstoque.comprador.nome);

                        if (estoque.saidaEstoque.comprador.endereco != null)
                        {
                            parametros.Add("@Bairro", estoque.saidaEstoque.comprador.endereco.bairro);
                            parametros.Add("@CEP", estoque.saidaEstoque.comprador.endereco.cep);
                            parametros.Add("@Complemento", estoque.saidaEstoque.comprador.endereco.complemento);
                            parametros.Add("@Logradouro", estoque.saidaEstoque.comprador.endereco.logradouro);
                            parametros.Add("@Numero", estoque.saidaEstoque.comprador.endereco.numero);

                            if (estoque.saidaEstoque.comprador.endereco.municipio != null)
                            {
                                parametros.Add("@CodigoMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.id);
                                parametros.Add("@NomeMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.nome);
                                parametros.Add("@UFMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.uf);
                            }
                        }
                    }
                }
            }

            parametros.Add("@CnpjConcessionariaVendedora", solicitacao.entregas.cnpjConcessionariaVendedora);
            parametros.Add("@CnpjConcessionariaEntregadora", solicitacao.entregas.cnpjConcessionariaEntregadora);
            parametros.Add("@TipoBeneficioTributario", solicitacao.tipoBeneficioTributario);

            new Dao().ExecutarProcedure("stp_Est_SaidaInacabadoDeMontadoraRenovar_Upd", parametros);
        }

        private void SalvarDadosServidorSolicitacaoSaidaIteRenovar(EstoqueJson estoque, TransacaoPendenteEnvio envio, string idTransacao)
        {
			SolicitacaoSaidaEstoqueRenovarIte solicitacao = JsonConvert.DeserializeObject<SolicitacaoSaidaEstoqueRenovarIte>(envio.jsonEnvio);

			Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idSessao", "-1");
            parametros.Add("@idProcesso", envio.idProcesso);
            //parametros.Add("@EmailComprador", _request.Comprador.email);
            //parametros.Add("@EmailEstabelecimento", _request.EmailEstabelecimento);
            parametros.Add("@EstabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar", solicitacao.estabelecimentoCienteDeResponsabilidadeSobreProcessoRenovar);
            //parametros.Add("@ValorVenda", _request.ValorVenda.ToString());
            parametros.Add("@idTransacao", idTransacao);

            if (estoque != null)
            {
                parametros.Add("@Estado", estoque.estado);

                if (estoque.saidaEstoque != null)
                {
                    parametros.Add("@ChaveNotaFiscalSaida", estoque.saidaEstoque.chaveNotaFiscalSaida);
                    //parametros.Add("@ChaveNotaFiscalProduto", estoque.saidaEstoque.ChaveNotaFiscalProduto);
                    parametros.Add("@ChaveNotaFiscalServico", estoque.saidaEstoque.chaveNotaFiscalServicoSaida);
                    parametros.Add("@CpfOperadorResponsavelSaidaEstoque", estoque.saidaEstoque.cpfOperadorResponsavel);
                    parametros.Add("@DataVenda", Util.FormatarData(estoque.saidaEstoque.dataHora, 120));
                    parametros.Add("@DataEnvioNotaFiscalSaida", Util.FormatarData(estoque.saidaEstoque.dataHoraEnvioNotaFiscalSaida, 120));

                    if (estoque.saidaEstoque.comprador != null)
                    {
                        parametros.Add("@TipoDocumentoComprador", estoque.saidaEstoque.comprador.tipoDocumento);
                        parametros.Add("@DocumentoComprador", estoque.saidaEstoque.comprador.numeroDocumento);
                        parametros.Add("@NomeComprador", estoque.saidaEstoque.comprador.nome);

                        if (estoque.saidaEstoque.comprador.endereco != null)
                        {
                            parametros.Add("@Bairro", estoque.saidaEstoque.comprador.endereco.bairro);
                            parametros.Add("@CEP", estoque.saidaEstoque.comprador.endereco.cep);
                            parametros.Add("@Complemento", estoque.saidaEstoque.comprador.endereco.complemento);
                            parametros.Add("@Logradouro", estoque.saidaEstoque.comprador.endereco.logradouro);
                            parametros.Add("@Numero", estoque.saidaEstoque.comprador.endereco.numero);

                            if (estoque.saidaEstoque.comprador.endereco.municipio != null)
                            {
                                parametros.Add("@CodigoMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.id);
                                parametros.Add("@NomeMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.nome);
                                parametros.Add("@UFMunicipio", estoque.saidaEstoque.comprador.endereco.municipio.uf);
                            }
                        }
                    }
                }
            }

            new Dao().ExecutarProcedure("stp_Est_SaidaITERenovar_Upd", parametros);
        }

        private IEnumerable<TransacaoPendenteEnvio> ConsultaVeiculosPendentesDeEnvio()
        {
            Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@IdSessao", -1);

            return new Dao().ExecutarProcedureList<TransacaoPendenteEnvio>("stp_Job_TransacaoRenave_Sel", parametros);
        }

        private void WsPdfAtpv(TransacaoPendenteEnvio transacao)
        {
            try
            {
                ComunicadorRenaveRequest envio = null;

                envio = new ComunicadorRenaveRequest()
                {
                    Token = Util.GetToken(),
                    IdEstabelecimento = transacao.idEstabelecimento,
                    CodigoTransacao = "pdf-atpv",
                    Url = $"{Util.BaseAddressWsRenave()}/pdf-atpv?chassi={transacao.chassi}",
                    MetodoRequisicao = "GET",
                    IdTransacao = "",
                    JsonEnvio = JsonConvert.SerializeObject(new
                    {
                        chassi = transacao.chassi
                    }),
                    JsonRetorno = "",
                    IdProcesso = transacao.idProcesso,
                    IdSessao = "-1"
                };


                string jsonRetorno = new DespachanteRenave().Send(envio, out string idTransacao);

                PdfAtpv pdfAtpv = null;

                try
                {
                    pdfAtpv = JsonConvert.DeserializeObject<PdfAtpv>(jsonRetorno);
                }
                catch (Exception)
                {

                    throw new Exception(jsonRetorno);
                }

                if (pdfAtpv == null || pdfAtpv.pdfAtpvBase64 == null)
                {
                    throw new InvalidOperationException("Não foi possível gerar o arquivo, Retorno da transação incompatível com o esperado");
                }

                CriaArquivoATPV(pdfAtpv, transacao);

                SalvarDadosServidorPdfAtpv(pdfAtpv, transacao);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"WSRenave - Erro na solicitação do PDF ATPV - {ex.Message}");
            }
        }

        private void WsPdfAtpvMontadora(TransacaoPendenteEnvio transacao)
        {
            try
            {
                ComunicadorRenaveRequest envio = null;

                envio = new ComunicadorRenaveRequest()
                {
                    Token = Util.GetToken(),
                    IdEstabelecimento = transacao.idEstabelecimento,
                    CodigoTransacao = "pdf-atpv",
                    Url = $"{Util.BaseAddressWsRenave()}/montadora/pdf-atpv?chassi={transacao.chassi}",
                    MetodoRequisicao = "GET",
                    IdTransacao = "",
                    JsonEnvio = JsonConvert.SerializeObject(new
                    {
                        chassi = transacao.chassi
                    }),
                    JsonRetorno = "",
                    IdProcesso = transacao.idProcesso,
                    IdSessao = "-1"
                };


                string jsonRetorno = new DespachanteRenave().Send(envio, out string idTransacao);

                PdfAtpv pdfAtpv = null;

                try
                {
                    pdfAtpv = JsonConvert.DeserializeObject<PdfAtpv>(jsonRetorno);
                }
                catch (Exception)
                {

                    throw new Exception(jsonRetorno);
                }

                if (pdfAtpv == null || pdfAtpv.pdfAtpvBase64 == null)
                {
                    throw new InvalidOperationException("Não foi possível gerar o arquivo, Retorno da transação incompatível com o esperado");
                }

                CriaArquivoATPV(pdfAtpv, transacao);

                SalvarDadosServidorPdfAtpv(pdfAtpv, transacao);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"WSRenave - Erro na solicitação do PDF ATPV - {ex.Message}");
            }
        }

        private void WsPdfAtpvIte(TransacaoPendenteEnvio transacao)
        {
            try
            {
                ComunicadorRenaveRequest envio = null;

                envio = new ComunicadorRenaveRequest()
                {
                    Token = Util.GetToken(),
                    IdEstabelecimento = transacao.idEstabelecimento,
                    CodigoTransacao = "pdf-atpv",
                    Url = $"{Util.BaseAddressWsRenave()}/ite/pdf-atpv?chassi={transacao.chassi}",
                    MetodoRequisicao = "GET",
                    IdTransacao = "",
                    JsonEnvio = JsonConvert.SerializeObject(new
                    {
                        chassi = transacao.chassi
                    }),
                    JsonRetorno = "",
                    IdProcesso = transacao.idProcesso,
                    IdSessao = "-1"
                };


                string jsonRetorno = new DespachanteRenave().Send(envio, out string idTransacao);

                PdfAtpv pdfAtpv = null;

                try
                {
                    pdfAtpv = JsonConvert.DeserializeObject<PdfAtpv>(jsonRetorno);
                }
                catch (Exception)
                {

                    throw new Exception(jsonRetorno);
                }

                if (pdfAtpv == null || pdfAtpv.pdfAtpvBase64 == null)
                {
                    throw new InvalidOperationException("Não foi possível gerar o arquivo, Retorno da transação incompatível com o esperado");
                }

                CriaArquivoATPV(pdfAtpv, transacao);

                SalvarDadosServidorPdfAtpv(pdfAtpv, transacao);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"WSRenave - Erro na solicitação do PDF ATPV - {ex.Message}");
            }
        }

        private void CriaArquivoATPV(PdfAtpv Atpv, TransacaoPendenteEnvio transacao)
        {
            string nomeArquivo = "Atpv_" + Atpv.numeroAtpv + ".pdf";

            caminhoLogicoPdfAtpv = Util.GetDiretorioArquivo(nomeArquivo, "Logico", transacao.idProcesso, Convert.ToString(transacao.idEstabelecimento));
            caminhoFisicoPdfAtpv = Util.GetDiretorioArquivo(nomeArquivo, "Fisico", transacao.idProcesso, Convert.ToString(transacao.idEstabelecimento));

            Util.CriaArquivo(
                caminhoFisico: caminhoFisicoPdfAtpv,
                pdfBase64: Atpv.pdfAtpvBase64
            );
        }

        private void SalvarDadosServidorPdfAtpv(PdfAtpv pdfAtpv, TransacaoPendenteEnvio transacao)
        {
            Dictionary<string, object> parametros = new Dictionary<string, object>
            {
                { "@idSessao", "-1" },
                { "@idProcesso", transacao.idProcesso },
                { "@numeroAtpv", pdfAtpv.numeroAtpv},
                { "@pdfAtpv", "PdfAtpv_" + pdfAtpv.numeroAtpv + ".pdf" },
                { "@pdfAtpvBase64", pdfAtpv.pdfAtpvBase64 }
            };

            new Dao().ExecutarProcedure("stp_Est_PdfAtpvSaida_upd", parametros);
        }

        private void AtualizarMonitor()
        {
            Dictionary<string, object> parametros = new Dictionary<string, object>();
            parametros.Add("@idMonitor", 3);

            new Dao().ExecutarProcedure("stp_Sys_Monitor_Upd", parametros);
        }

        private void Log(string msg)
        {
            if (!Directory.Exists(diretorioLog))
                Directory.CreateDirectory(diretorioLog);

            string filename = Path.Combine(diretorioLog, "log.log");

            using FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), msg));

            sw.Close();
            fs.Close();
        }

		private void RealizarMovimentacaoFinanceira(TipoOperacaoFinanceira tipoOperacao, int tipoSaida, TransacaoPendenteEnvio item)
		{
			Dictionary<string, object> parametros = new Dictionary<string, object>
			{
				{ "@idProcesso",  item.idProcesso },
				{ "@TipoOperacao",  (int)tipoOperacao },
				{ "@idSessao", "-1"},
				{ "@TipoSaida", tipoSaida }
			};

			// RMF - Realizar Movimentação Financeira do processo (stp_Arr_RealizarMovimentacaoFinanceira)
			new Dao().ExecutarProcedure("stp_Arr_RealizarMovimentacaoFinanceira", parametros);
		}
	}
}
