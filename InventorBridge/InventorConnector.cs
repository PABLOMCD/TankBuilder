using System;
using System.IO;
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

            return _invApp.FileManager.GetTemplateFile(
                DocumentTypeEnum.kPartDocumentObject,
                SystemOfMeasureEnum.kDefaultSystemOfMeasure,
                DraftingStandardEnum.kDefault_DraftingStandard,
                SHEET_METAL_GUID);
        }

        public void CreateTankSheetMetal(TankModel tank)
        {
            double widthMm = UnitConverter.ToMillimeters(tank.Width);
            double heightMm = UnitConverter.ToMillimeters(tank.Height);
            double thickMm = UnitConverter.ToMillimeters(tank.Thickness);

            var partDoc = (PartDocument)_invApp.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject,
                GetCorporateSheetMetalTemplate(),
                true);

            var smDef = partDoc.ComponentDefinition as SheetMetalComponentDefinition
                        ?? throw new InvalidOperationException("La plantilla no es de Sheet Metal.");

            var uom = partDoc.UnitsOfMeasure;
            smDef.Thickness.Value = (double)uom.GetValueFromExpression($"{thickMm} mm", UnitsTypeEnum.kDefaultDisplayLengthUnits);

            var tg = _invApp.TransientGeometry;
            var sk = smDef.Sketches.Add(smDef.WorkPlanes[3]);

            double wx = (double)uom.GetValueFromExpression($"{widthMm} mm", UnitsTypeEnum.kDefaultDisplayLengthUnits);
            double hy = (double)uom.GetValueFromExpression($"{heightMm} mm", UnitsTypeEnum.kDefaultDisplayLengthUnits);

            sk.SketchLines.AddAsTwoPointRectangle(tg.CreatePoint2d(0, 0), tg.CreatePoint2d(wx, hy));

            var prof = sk.Profiles.AddForSolid();
            var smFeat = (SheetMetalFeatures)smDef.Features;
            var faceDef = smFeat.FaceFeatures.CreateFaceFeatureDefinition(prof);
            smFeat.FaceFeatures.Add(faceDef);

            System.IO.Directory.CreateDirectory(@"C:\Temp");
            partDoc.SaveAs(@"C:\Temp\TankBuilder_SheetBase.ipt", false);
        }
    }
}
