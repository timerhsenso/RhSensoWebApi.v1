(function () {
  const root = document.querySelector("section.container");
  const id = root?.dataset?.id;
  const btnSalvar = document.getElementById("btnSalvar");
  const btnExcluir = document.getElementById("btnExcluir");
  const btnReset   = document.getElementById("btnReset");

  function getVal(id) { return document.getElementById(id).value; }
  function getInt(id) { const v = getVal(id).trim(); return v ? parseInt(v,10) : null; }
  function getStrOrNull(id) { const v = getVal(id).trim(); return v ? v : null; }

  async function salvar() {
    const base = {
      Descricao: getVal("Descricao").trim(),
      Tipo: parseInt(getVal("Tipo"),10),
      SenhaUser: getStrOrNull("SenhaUser"),
      NomeImpCheque: getStrOrNull("NomeImpCheque"),
      NoMatric: getStrOrNull("NoMatric"),
      CdEmpresa: getInt("CdEmpresa"),
      CdFilial: getInt("CdFilial"),
      NoUser: parseInt(getVal("NoUser"),10),
      Email: getStrOrNull("Email"),
      Ativo: getVal("Ativo"),
      NormalizedUserName: getStrOrNull("NormalizedUserName"),
      IdFuncionario: getStrOrNull("IdFuncionario"),
      FlNaoRecebeEmail: getStrOrNull("FlNaoRecebeEmail")
    };

    let url, method, body;
    if (!id) {
      const dto = { Codigo: getVal("Codigo").trim().toUpperCase(), ...base };
      url = `/SEG/Usuarios/Create`; method = "POST"; body = JSON.stringify(dto);
    } else {
      const dto = { ...base };
      url = `/SEG/Usuarios/Update?id=${encodeURIComponent(id)}`; method = "PUT"; body = JSON.stringify(dto);
    }

    const res = await fetch(url, { method, headers: {"Content-Type": "application/json"}, body });
    if (res.ok) {
      alert("Salvo!");
      window.location.href = "/SEG/Usuarios";
    } else {
      const err = await res.text();
      alert("Falhou: " + err);
    }
  }

  async function excluir() {
    if (!confirm("Excluir este usuário?")) return;
    const res = await fetch(`/SEG/Usuarios/Delete?id=${encodeURIComponent(id)}`, { method: "DELETE" });
    if (res.ok) { alert("Excluído."); window.location.href = "/SEG/Usuarios"; }
    else { alert("Falhou excluir."); }
  }

  async function resetSenha() {
    if (!confirm("Redefinir senha para a padrão?")) return;
    const res = await fetch(`/SEG/Usuarios/ResetPassword?id=${encodeURIComponent(id)}`, { method: "POST" });
    if (res.ok) { alert("Senha redefinida."); }
    else { alert("Falhou."); }
  }

  async function carregar() {
    if (!id) return;
    const res = await fetch(`/SEG/Usuarios/GetOne?id=${encodeURIComponent(id)}`);
    if (!res.ok) return;
    const x = await res.json();
    document.getElementById("Codigo").value = x.codigo;
    document.getElementById("Descricao").value = x.descricao;
    document.getElementById("Tipo").value = x.tipoStr === "Prestador de Serviço" ? "0" : "1";
    document.getElementById("Email").value = x.email ?? "";
    document.getElementById("Ativo").value = x.situacao === "Ativo" ? "S" : "N";
    document.getElementById("NoUser").value = x.noUser ?? "";
    document.getElementById("CdEmpresa").value = x.cdEmpresa ?? "";
    document.getElementById("CdFilial").value = x.cdFilial ?? "";
    const idField = document.getElementById("Id"); if (idField) idField.value = ""; // Id não é retornado nesse endpoint
  }

  btnSalvar?.addEventListener("click", salvar);
  btnExcluir?.addEventListener("click", excluir);
  btnReset?.addEventListener("click", resetSenha);
  carregar();
})();