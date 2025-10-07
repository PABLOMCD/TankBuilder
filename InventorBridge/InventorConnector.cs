using System;
using Inventor;
using CoreLogic.Models;
using CoreLogic.Utils;

namespace InventorBridge
{
    public class InventorConnector
    {
        private Inventor.Application _invApp;

        public InventorConnector()
        {
            try
            {
                // Intenta conectarse a una sesión abierta
                object obj = System.Activator.CreateInstance(Type.GetTypeFromProgID("Inventor.Application"));
                _invApp = (Inventor.Application)obj;
                _invApp.Visible = true;
            }
            catch (Exception)
            {
                // Si no se puede, abre Inventor
                Type inventorAppType = Type.GetTypeFromProgID("Inventor.Application");
                _invApp = (Inventor.Application)Activator.CreateInstance(inventorAppType);
                _invApp.Visible = true;
            }
        }



        public void CreateTankPart(TankModel tank)
        {
            // Convierte dimensiones de pulgadas a milímetros
            double width = UnitConverter.ToMillimeters(tank.Width);
            double height = UnitConverter.ToMillimeters(tank.Height);
            double depth = UnitConverter.ToMillimeters(tank.Depth);
            double thickness = UnitConverter.ToMillimeters(tank.Thickness);

            // Crea un nuevo documento de pieza
            PartDocument partDoc = (PartDocument)_invApp.Documents.Add(DocumentTypeEnum.kPartDocumentObject,
                _invApp.FileManager.GetTemplateFile(DocumentTypeEnum.kPartDocumentObject),
                true);

            PartComponentDefinition compDef = partDoc.ComponentDefinition;
            TransientGeometry tg = _invApp.TransientGeometry;

            // Crear un sketch básico para el perfil del tanque
            PlanarSketch sketch = compDef.Sketches.Add(compDef.WorkPlanes[3]);
            sketch.SketchLines.AddAsTwoPointRectangle(
                tg.CreatePoint2d(0, 0),
                tg.CreatePoint2d(width, height)
            );

            // Extruir para generar el tanque base
            Profile profile = sketch.Profiles.AddForSolid();
            compDef.Features.ExtrudeFeatures.AddByDistanceExtent(
                profile, depth,
                PartFeatureExtentDirectionEnum.kPositiveExtentDirection,
                PartFeatureOperationEnum.kJoinOperation);

            partDoc.SaveAs(@"C:\Temp\TankBuilder_Sample.ipt", false);
        }
    }
}
