﻿// -----------------------------------------------------------------------
// <copyright file="Version.cs" company="Nodine Legal, LLC">
// Licensed to Nodine Legal, LLC under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  Nodine Legal, LLC licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
// </copyright>
// -----------------------------------------------------------------------

namespace OpenLawOffice.Data.DBOs.Documents
{
    using System;
    using AutoMapper;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Common.Models.MapMe]
    public class Version : Core
    {
        [ColumnMapping(Name = "id")]
        public Guid Id { get; set; }

        [ColumnMapping(Name = "document_id")]
        public Guid DocumentId { get; set; }

        [ColumnMapping(Name = "version_number")]
        public int VersionNumber { get; set; }

        [ColumnMapping(Name = "mime")]
        public string Mime { get; set; }

        [ColumnMapping(Name = "filename")]
        public string Filename { get; set; }

        [ColumnMapping(Name = "extension")]
        public string Extension { get; set; }

        [ColumnMapping(Name = "size")]
        public long Size { get; set; }

        [ColumnMapping(Name = "md5")]
        public string Md5 { get; set; }

        public void BuildMappings()
        {
            Dapper.SqlMapper.SetTypeMap(typeof(Version), new ColumnAttributeTypeMapper<Version>());
            Mapper.CreateMap<DBOs.Documents.Version, Common.Models.Documents.Version>()
                .ForMember(dst => dst.IsStub, opt => opt.UseValue(false))
                .ForMember(dst => dst.Created, opt => opt.ResolveUsing(db =>
                {
                    return db.UtcCreated.ToSystemTime();
                }))
                .ForMember(dst => dst.Modified, opt => opt.ResolveUsing(db =>
                {
                    return db.UtcModified.ToSystemTime();
                }))
                .ForMember(dst => dst.Disabled, opt => opt.ResolveUsing(db =>
                {
                    return db.UtcDisabled.ToSystemTime();
                }))
                .ForMember(dst => dst.CreatedBy, opt => opt.ResolveUsing(db =>
                {
                    return new Common.Models.Account.Users()
                    {
                        PId = db.CreatedByUserPId,
                        IsStub = true
                    };
                }))
                .ForMember(dst => dst.ModifiedBy, opt => opt.ResolveUsing(db =>
                {
                    return new Common.Models.Account.Users()
                    {
                        PId = db.ModifiedByUserPId,
                        IsStub = true
                    };
                }))
                .ForMember(dst => dst.DisabledBy, opt => opt.ResolveUsing(db =>
                {
                    if (!db.DisabledByUserPId.HasValue) return null;
                    return new Common.Models.Account.Users()
                    {
                        PId = db.DisabledByUserPId.Value,
                        IsStub = true
                    };
                }))
                .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dst => dst.Document, opt => opt.ResolveUsing(db =>
                {
                    return new Common.Models.Documents.Document()
                    {
                        Id = db.DocumentId,
                        IsStub = true
                    };
                }))
                .ForMember(dst => dst.VersionNumber, opt => opt.MapFrom(src => src.VersionNumber))
                .ForMember(dst => dst.Mime, opt => opt.MapFrom(src => src.Mime))
                .ForMember(dst => dst.Filename, opt => opt.MapFrom(src => src.Filename))
                .ForMember(dst => dst.Extension, opt => opt.MapFrom(src => src.Extension))
                .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Size))
                .ForMember(dst => dst.Md5, opt => opt.MapFrom(src => src.Md5));

            Mapper.CreateMap<Common.Models.Documents.Version, DBOs.Documents.Version>()
                .ForMember(dst => dst.UtcCreated, opt => opt.ResolveUsing(db =>
                {
                    return db.Created.ToDbTime();
                }))
                .ForMember(dst => dst.UtcModified, opt => opt.ResolveUsing(db =>
                {
                    return db.Modified.ToDbTime();
                }))
                .ForMember(dst => dst.UtcDisabled, opt => opt.ResolveUsing(db =>
                {
                    return db.Disabled.ToDbTime();
                }))
                .ForMember(dst => dst.CreatedByUserPId, opt => opt.ResolveUsing(model =>
                {
                    if (model.CreatedBy == null || !model.CreatedBy.PId.HasValue)
                        return Guid.Empty;
                    return model.CreatedBy.PId.Value;
                }))
                .ForMember(dst => dst.ModifiedByUserPId, opt => opt.ResolveUsing(model =>
                {
                    if (model.ModifiedBy == null || !model.ModifiedBy.PId.HasValue)
                        return Guid.Empty;
                    return model.ModifiedBy.PId.Value;
                }))
                .ForMember(dst => dst.DisabledByUserPId, opt => opt.ResolveUsing(model =>
                {
                    if (model.DisabledBy == null) return null;
                    return model.DisabledBy.PId;
                }))
                .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dst => dst.DocumentId, opt => opt.ResolveUsing(model =>
                {
                    if (model.Document == null || !model.Document.Id.HasValue)
                        return 0;
                    return model.Document.Id.Value;
                }))
                .ForMember(dst => dst.VersionNumber, opt => opt.MapFrom(src => src.VersionNumber))
                .ForMember(dst => dst.Mime, opt => opt.MapFrom(src => src.Mime))
                .ForMember(dst => dst.Filename, opt => opt.MapFrom(src => src.Filename))
                .ForMember(dst => dst.Extension, opt => opt.MapFrom(src => src.Extension))
                .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Size))
                .ForMember(dst => dst.Md5, opt => opt.MapFrom(src => src.Md5));
        }
    }
}