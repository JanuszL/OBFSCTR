﻿using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Obfuscator.Utils;
using Mono.Cecil.Cil;
using System.IO;

namespace Obfuscator.Renaming
{
    public partial class RenameMapVisitor : NullAssemblyVisitor
    {       
        private IFilter _filter;
        private Renamer _renamer;

        public RenameMapVisitor(Renamer renamer, IFilter filter)
        {
            _renamer = renamer;
            _filter = filter;
        }

        public override VisitorLevel Level()
        {
            return VisitorLevel.MethodBodys;
        }

        public override void VisitTypeDefinition(Mono.Cecil.TypeDefinition type)
        {
            if (!ShouldBeRenamed(type))
                return;

            logVisitingMember(type);
            RenameDefinition(type);
        }

        public override void VisitMethodDefinition(Mono.Cecil.MethodDefinition method)
        {
            if (!ShouldBeRenamed(method))
                return;

            logVisitingMember(method);
            RenameDefinition(method);
        }

        public override void VisitFieldDefinition(Mono.Cecil.FieldDefinition field)
        {
            if (!ShouldBeRenamed(field))
                return;

            logVisitingMember(field);
            RenameDefinition(field);
        }

        public override void VisitEventDefinition(Mono.Cecil.EventDefinition @event)
        {
            if (!ShouldBeRenamed(@event))
                return;

            logVisitingMember(@event);
            RenameDefinition(@event);
        }

        public override void VisitPropertyDefinition(Mono.Cecil.PropertyDefinition property)
        {
            if (!ShouldBeRenamed(property))
                return;

            logVisitingMember(property);
            RenameDefinition(property);
        }

        public override void VisitParameterDefinition(ParameterDefinition parameter)
        {
            parameter.Name = String.Empty;
        }

        // Variable names don't matter for the runtime. They are being stored for readability.
        // In CIL variables are indexed accessed by location.
        public override void VisitVariableDefinition(VariableDefinition variable)
        {
            variable.Name = String.Empty;
        }

        //public override void VisitInstruction(Instruction instruction)
        //{
        //    if (instruction.OpCode == OpCodes.Ldstr)
        //    {
        //        _renamer.MapInstruction(instruction);
        //    }
        //}        

        public override void VisitResource(Resource resource)
        {
            var newName = _renamer.MapResource(resource);
            logRenamingResource(resource, newName);
        }



        public override void VisitEventReference(Mono.Cecil.EventReference @event)
        {
            RenameReference(@event);

            //IMemberDefinition definition = @event.Resolve();
            //var assemblyName = GetAssemblyName(definition);

            //if (Visited(@event, assemblyName))
            //    return;

            //ResolvedMembers[@event] = definition;
            //Visit(@event, assemblyName);
        }


        public override void VisitFieldReference(Mono.Cecil.FieldReference field)
        {
            RenameReference(field);

            //IMemberDefinition definition = field.Resolve();
            //var assemblyName = GetAssemblyName(definition);

            //if (Visited(field, assemblyName))
            //    return;

            //ResolvedMembers[field] = definition;
            //Visit(field, assemblyName);
        }

        public override void VisitMethodReference(Mono.Cecil.MethodReference method)
        {
            RenameReference(method);

            //IMemberDefinition definition = method.Resolve();
            //var assemblyName = GetAssemblyName(definition);

            //if (Visited(method, assemblyName))
            //    return;

            //ResolvedMembers[method] = definition;
            //Visit(method, assemblyName);
        }

        public override void VisitPropertyReference(Mono.Cecil.PropertyReference property)
        {
            RenameReference(property);

            //IMemberDefinition definition = property.Resolve();
            //var assemblyName = GetAssemblyName(definition);

            //if (Visited(property, assemblyName))
            //    return;

            //ResolvedMembers[property] = definition;
            //Visit(property, assemblyName);
        }

        public override void VisitTypeReference(Mono.Cecil.TypeReference type)
        {
            RenameReference(type);

            //IMemberDefinition definition = type.Resolve();
            //var assemblyName = GetAssemblyName(definition);

            //if (Visited(type, assemblyName))
            //    return;

            //ResolvedMembers[type] = definition;
            //Visit(type, assemblyName);
        }

        private void RenameReference(MemberReference reference)
        {
            _renamer.MapReference(reference);
        } 

        private void RenameDefinition(IMemberDefinition definition)
        {
            var newName = _renamer.MapDefinition(definition);
            logRenamingDefinition(definition, newName);
        }

        private bool ShouldSkip(IMemberDefinition member)
        {
            return _filter.ShouldSkip(member);
        }

        private bool ShouldBeRenamed(MethodDefinition method)
        {
            if (ShouldSkip(method))
            {
                logSkipingMember(method, SKIPPING_SKIPPED);
                return false;
            }
            if (method.IsConstructor)
            {
                logSkipingMember(method, SKIPPING_CONSTRUCTOR);
                return false;
            }
            if (method.IsSpecialName &&
                !(method.IsGetter || method.IsSetter || method.IsRemoveOn || method.IsAddOn))
            {
                logSkipingMember(method, SKIPPING_SPECIAL_NAME);
                return false;
            }
            if (method.IsRuntime)
            {
                logSkipingMember(method, SKIPPING_RUNTIME);
                return false;
            }
            return true;
        }


        private bool ShouldBeRenamed(TypeDefinition type)
        {
            if (type.Name == "<Module>")
                return false;

            if (ShouldSkip(type))
            {
                logSkipingMember(type, SKIPPING_SKIPPED);
                return false;
            }
            return true;
        }

        private bool ShouldBeRenamed(FieldDefinition field)
        {
            if (ShouldSkip(field))
                return false;

            if (field.IsSpecialName || field.IsRuntimeSpecialName)
            {
                logSkipingMember(field, SKIPPING_SPECIAL_NAME);
                return false;
            }
            return true;
        }

        private bool ShouldBeRenamed(PropertyDefinition property)
        {
            if (ShouldSkip(property))
                return false;
            if (property.IsSpecialName || property.IsRuntimeSpecialName)
            {
                logSkipingMember(property, SKIPPING_SPECIAL_NAME);
                return false;
            }
            return true;
        }

        private bool ShouldBeRenamed(EventDefinition @event)
        {
            if (ShouldSkip(@event))
                return false;
            if (@event.IsSpecialName || @event.IsRuntimeSpecialName)
            {
                logSkipingMember(@event, SKIPPING_SPECIAL_NAME);
                return false;
            }

            return true;
        }

        #region Logging
        const string SKIPPING_SKIPPED = "IS SKIPPED";
        const string SKIPPING_CONSTRUCTOR = "IS A CONSTRUCTOR"; // TODO isn't constructor a special name
        const string SKIPPING_SPECIAL_NAME = "IS A SPECIAL NAME";
        const string SKIPPING_RUNTIME = "IS RUNTIME";
        const string SKIPPING_EXTERNAL_VIRTUAL = "IS EXTERNAL VIRTUAL";

        partial void logVisitingMember(IMemberDefinition member);
        partial void logSkipingMember(IMemberDefinition member, string message);
        partial void logRenamingDefinition(IMemberDefinition member, string newName);
        partial void logRenamingReference(MemberReference reference, string newName);        
        partial void logRenamingResource(Resource resource, string newName);
        #endregion
    }
}