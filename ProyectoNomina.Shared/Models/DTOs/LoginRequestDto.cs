﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoNomina.Shared.Models.DTOs
{
    public class LoginRequestDto
    {
        public string Correo { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
    }
}
