using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Inventor;
using CoreLogic.Models;
using CoreLogic.Utils;

// Alias para evitar ambigüedades con Inventor.Environment y Inventor.File
using SysEnv = global::System.Environment;
using IOFile = global::System.IO.File;

namespace InventorBridge
{
    public class InventorConnector
    {
        private Inventor.Application _invApp;
        private const string SHEET_METAL_GUID = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";
        private const string VTC_INV_SM_TEMPLATE_FALLBACK =
            @"V:\3D\Inventor Configuration\2020 Templates\Corporate Standard\VTC - Sheet Metal.ipt";

        public InventorConnector()
        {
            try { _invApp = (Inventor.Application)Marshal.BindToMoniker("!Inventor.Application"); }
            catch
            {
                var t = Type.GetTypeFromProgID("Inventor.Application");
                _invApp = (Inventor.Application)Activator.CreateInstance(t);
                _invApp.Visible = true;
            }
        }

        private string GetCorporateSheetMetalTemplate()
        {
            string env = SysEnv.GetEnvironmentVariable("VTC_INV_SM_TEMPLATE");
            if (!string.IsNullOrWhiteSpace(env) && IOFile.Exists(env)) return env;

            if (IOFile.Exists(VTC_INV_SM_TEMPLATE_FALLBACK)) return VTC_INV_SM_TEMPLATE_FALLBACK;

            // Forzar plantilla de Sheet Metal por GUID (fallback universal)
            return _invApp.FileManager.GetTemplateFile(
                DocumentTypeEnum.kPartDocumentObject,
                SystemOfMeasureEnum.kDefaultSystemOfMeasure,
                DraftingStandardEnum.kDefault_DraftingStandard,
                SHEET_METAL_GUID);
        }

        // --- Consulta de estilos desde la PLANTILLA corporativa ---
        public IList<string> GetSheetMetalStyles()
        {
            var doc = (PartDocument)_invApp.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                GetCorporateSheetMetalTemplate(),
                true);

            try
            {
                var smDef = doc.ComponentDefinition as SheetMetalComponentDefinition
                    ?? throw new InvalidOperationException("La plantilla no es de Sheet Metal.");

                var estilos = new List<string>();
                foreach (SheetMetalStyle s in smDef.SheetMetalStyles) estilos.Add(s.Name);
                return estilos;
            }
            finally { doc.Close(true); }
        }

        // --- Consulta de reglas de desplegado ---
        public IList<string> GetUnfoldRules()
        {
            var doc = (PartDocument)_invApp.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                GetCorporateSheetMetalTemplate(),
                true);

            try
            {
                var smDef = doc.ComponentDefinition as SheetMetalComponentDefinition
                    ?? throw new InvalidOperationException("La plantilla no es de Sheet Metal.");

                var reglas = new List<string>();
                foreach (UnfoldMethod m in smDef.UnfoldMethods) reglas.Add(m.Name);
                return reglas;
            }
            finally { doc.Close(true); }
        }

        // =======================
        //  MODIFICAR PIEZA EXISTENTE
        // =======================
        public void ModifyTankSheetMetal(string partPath, TankModel tank)
        {
            if (string.IsNullOrWhiteSpace(partPath))
                throw new ArgumentException("Ruta de la pieza no válida.", nameof(partPath));
            if (!IOFile.Exists(partPath))
                throw new FileNotFoundException("No se encontró la pieza a modificar.", partPath);

            var partDoc = (PartDocument)_invApp.Documents.Open(partPath, true)
                ?? throw new InvalidOperationException("No se pudo abrir el documento.");

            // StartTransaction requiere _Document
            var txn = _invApp.TransactionManager.StartTransaction(
                (Inventor._Document)partDoc, "TankBuilder: Modify SheetMetal");

            try
            {
                var smDef = partDoc.ComponentDefinition as SheetMetalComponentDefinition
                    ?? throw new InvalidOperationException("El documento no es de Sheet Metal.");

                // Estilo y regla por variables de entorno (si existen)
                ActivatePreferredSheetMetalStyle(smDef);
                ApplyPreferredUnfoldRule(smDef);

                // Actualizar parámetros
                double widthMm = UnitConverter.ToMillimeters(tank.Width);
                double heightMm = UnitConverter.ToMillimeters(tank.Height);
                double thickMm = UnitConverter.ToMillimeters(tank.Thickness);

                TrySetAnyParameter(partDoc,
                    new[] { "Width", "Ancho", "Tank_Width", "PanelWidth" }, widthMm, "mm");

                TrySetAnyParameter(partDoc,
                    new[] { "Height", "Alto", "Tank_Height", "PanelHeight" }, heightMm, "mm");

                // Ojo: el espesor normalmente lo manda el Estilo de chapa
                TrySetAnyParameter(partDoc,
                    new[] { "Thickness", "Espesor", "t", "Gauge" }, thickMm, "mm");

                partDoc.Update2(true);
                partDoc.Save();

                txn.End();
            }
            catch
            {
                txn.Abort();
                throw;
            }
        }

        // Compatibilidad con tu firma previa: ahora lanza NotSupported
        public void CreateTankPart(TankModel tank)
        {
            throw new NotSupportedException(
                "Esta versión no crea piezas nuevas. Usa ModifyTankSheetMetal(partPath, tank) para modificar una existente.");
        }

        // =======================
        //  HELPERS
        // =======================
        private static bool TrySetAnyParameter(PartDocument doc, string[] candidateNames, double value, string units)
        {
            if (doc == null) return false;
            if (candidateNames == null || candidateNames.Length == 0) return false;

            var compDef = doc.ComponentDefinition;
            var parms = compDef.Parameters;

            // 1) UserParameters
            foreach (Parameter p in parms.UserParameters)
            {
                if (NameMatch(p.Name, candidateNames))
                {
                    p.Expression = $"{value} {units}";
                    return true;
                }
            }

            // 2) ModelParameters
            foreach (ModelParameter mp in parms.ModelParameters)
            {
                if (NameMatch(mp.Name, candidateNames))
                {
                    mp.Expression = $"{value} {units}";
                    return true;
                }
            }

            return false;
        }

        private static bool NameMatch(string candidate, string[] targets)
        {
            foreach (var t in targets)
                if (string.Equals(candidate, t, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void ActivatePreferredSheetMetalStyle(SheetMetalComponentDefinition smDef)
        {
            string preferredStyle = SysEnv.GetEnvironmentVariable("VTC_INV_SM_STYLE");
            if (string.IsNullOrWhiteSpace(preferredStyle)) return;

            SheetMetalStyle found = null;
            foreach (SheetMetalStyle s in smDef.SheetMetalStyles)
                if (string.Equals(s.Name, preferredStyle, StringComparison.OrdinalIgnoreCase))
                { found = s; break; }

            if (found == null)
                throw new InvalidOperationException($"Estilo de chapa no encontrado: {preferredStyle}");

            found.Activate();
        }

        private static void ApplyPreferredUnfoldRule(SheetMetalComponentDefinition smDef)
        {
            string ruleName = SysEnv.GetEnvironmentVariable("VTC_INV_UNFOLD_RULE");
            if (string.IsNullOrWhiteSpace(ruleName)) return;

            UnfoldMethod rule = null;
            foreach (UnfoldMethod m in smDef.UnfoldMethods)
                if (string.Equals(m.Name, ruleName, StringComparison.OrdinalIgnoreCase))
                { rule = m; break; }

            if (rule == null)
                throw new InvalidOperationException($"Regla de desplegado no encontrada: {ruleName}");

            smDef.UnfoldMethod = rule;
        }
    }
}
