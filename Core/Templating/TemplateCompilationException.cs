﻿using System;
using Microsoft.CodeAnalysis;

namespace ConsulRazor.Templating
{
    public class TemplateCompilationException : Exception
    {
        private Diagnostic[] _errors;

        public TemplateCompilationException(Diagnostic[] errors) : base("An exception ocurred compiling the template")
        {
            _errors = errors;
        }
    }
}