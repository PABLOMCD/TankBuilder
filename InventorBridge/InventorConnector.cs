using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Inventor;
using CoreLogic.Models;
using CoreLogic.Utils;

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
            string env = System.Environment.GetEnvironmentVariable("VTC_INV_SM_TEMPLATE");
            if (!string.IsNullOrWhiteSpace(env) && System.IO.File.Exists(env)) return env;

            if (System.IO.File.Exists(VTC_INV_SM_TEMPLATE_FALLBACK)) return VTC_INV_SM_TEMPLATE_FALLBACK;

            // Forzar plantilla de Sheet Metal por GUID
            return _invApp.FileManager.GetTemplateFile(
                DocumentTypeEnum.kPartDocumentObject,
                SystemOfMeasureEnum.kDefaultSystemOfMeasure,
                DraftingStandardEnum.kDefault_DraftingStandard,
                SHEET_METAL_GUID);
        }

        // Devuelve los nombres de estilos de chapa disponibles en la plantilla corporativa
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
            finally
            {
                doc.Close(true);
            }
        }

        // Devuelve los nombres de reglas de desplegado (Unfold Methods)
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
            finally
            {
                doc.Close(true);
            }
        }

        public void CreateTankSheetMetal(TankModel tank)
        {
            double widthMm = UnitConverter.ToMillimeters(tank.Width);
            double heightMm = UnitConverter.ToMillimeters(tank.Height);

            var partDoc = (PartDocument)_invApp.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                GetCorporateSheetMetalTemplate(),
                true);

            var smDef = partDoc.ComponentDefinition as SheetMetalComponentDefinition
                        ?? throw new InvalidOperationException("La plantilla no es de Sheet Metal.");

            // --- ACTIVAR ESTILO (no asignar la propiedad) ---
            string preferredStyle = System.Environment.GetEnvironmentVariable("VTC_INV_SM_STYLE");
            if (!string.IsNullOrWhiteSpace(preferredStyle))
            {
                SheetMetalStyle found = null;
                foreach (SheetMetalStyle s in smDef.SheetMetalStyles)
                    if (string.Equals(s.Name, preferredStyle, StringComparison.OrdinalIgnoreCase))
                    { found = s; break; }

                if (found == null)
                    throw new InvalidOperationException($"Estilo de chapa no encontrado: {preferredStyle}");

                found.Activate(); // clave
            }
            // Si no hay variable, se conserva el estilo activo de la plantilla.

            // (Opcional) Regla de desplegado por variable
            string ruleName = System.Environment.GetEnvironmentVariable("VTC_INV_UNFOLD_RULE");
            if (!string.IsNullOrWhiteSpace(ruleName))
            {
                UnfoldMethod rule = null;
                foreach (UnfoldMethod m in smDef.UnfoldMethods)
                    if (string.Equals(m.Name, ruleName, StringComparison.OrdinalIgnoreCase))
                    { rule = m; break; }
                if (rule == null)
                    throw new InvalidOperationException($"Regla de desplegado no encontrada: {ruleName}");
                smDef.UnfoldMethod = rule;
            }

            // Sketch y base
            var tg = _invApp.TransientGeometry;
            var sketch = smDef.Sketches.Add(smDef.WorkPlanes[3]);
            sketch.SketchLines.AddAsTwoPointRectangle(
                tg.CreatePoint2d(0, 0),
                tg.CreatePoint2d(widthMm, heightMm));

            var profile = sketch.Profiles.AddForSolid();
            var smFeat = (SheetMetalFeatures)smDef.Features;
            var faceDef = smFeat.FaceFeatures.CreateFaceFeatureDefinition(profile);
            smFeat.FaceFeatures.Add(faceDef);

            System.IO.Directory.CreateDirectory(@"C:\Temp");
            partDoc.SaveAs(@"C:\Temp\TankBuilder_SheetBase.ipt", false);
        }

        // Compatibilidad con llamadas existentes
        public void CreateTankPart(TankModel tank) => CreateTankSheetMetal(tank);
    }
}
