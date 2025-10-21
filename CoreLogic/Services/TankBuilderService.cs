using CoreLogic.Models;
using System;

namespace CoreLogic.Services
{
    public class TankBuilderService
    {
        public LeftWall Tank { get; private set; }

        public TankBuilderService(
            double alto,
            double largo,
            double flangein,
            double flangeang,
            double diamdv, double largodv, double altodv,
            double altoltg, double largoltg, double diamltg,
            double altolfv, double largolfv, double diamlfv,
            double altollg, double largollg, double diamllg,
            double altotp, double largotp, double diamtp,
            double altopvg, double largopvg, double diampvg
        )
        {
            // Crea el modelo con TODOS los valores proporcionados
            Tank = new LeftWall
            {
                ALTO = alto,
                LARGO = largo,
                FLANGEIN = flangein,
                FLANGEANG = flangeang,

                DIAMDV = diamdv,
                LARGODV = largodv,
                ALTODV = altodv,

                ALTOLTG = altoltg,
                LARGOLTG = largoltg,
                DIAMLTG = diamltg,

                ALTOLFV = altolfv,
                LARGOLFV = largolfv,
                DIAMLFV = diamlfv,

                ALTOLLG = altollg,
                LARGOLLG = largollg,
                DIAMLLG = diamllg,

                ALTOTP = altotp,
                LARGOTP = largotp,
                DIAMTP = diamtp,

                ALTOPVG = altopvg,
                LARGOPVG = largopvg,
                DIAMPVG = diampvg
            };

            // Validación opcional
            Validate();
        }

        private void Validate()
        {
            if (Tank.ALTO <= 0 || Tank.LARGO <= 0)
                throw new ArgumentException("El alto y el largo deben ser mayores que 0.");

            if (Tank.FLANGEIN <= 0)
                throw new ArgumentException("La pestaña (FLANGEIN) debe ser positiva.");

            if (Tank.DIAMDV <= 0 || Tank.DIAMLTG <= 0)
                throw new ArgumentException("Los diámetros de válvulas deben ser mayores que 0.");

            // Puedes agregar más validaciones específicas de diseño aquí
        }
    }
}
