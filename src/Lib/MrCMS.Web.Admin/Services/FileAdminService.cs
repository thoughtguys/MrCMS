using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.Data;
using MrCMS.Entities.Documents.Media;
using MrCMS.Helpers;
using MrCMS.Mapping;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Services.Resources;
using MrCMS.Settings;
using MrCMS.Web.Admin.Models;
using MrCMS.Web.Admin.Helpers;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Transform;
using X.PagedList;

namespace MrCMS.Web.Admin.Services
{
    public class FileAdminService : IFileAdminService
    {
        private readonly IFileService _fileService;
        private readonly MediaSettings _mediaSettings;
        private readonly IGetMediaCategoriesByParent _getMediaCategoriesByParent;
        private readonly ISessionAwareMapper _mapper;
        private readonly ISession _session;
        private readonly IStringResourceProvider _stringResourceProvider;
        private readonly IRepository<MediaCategory> _mediaCategoryRepository;

        public FileAdminService(IFileService fileService, ISession session,
            IStringResourceProvider stringResourceProvider, IRepository<MediaCategory> mediaCategoryRepository,
            MediaSettings mediaSettings, IGetMediaCategoriesByParent getMediaCategoriesByParent,
            ISessionAwareMapper mapper)
        {
            _fileService = fileService;
            _session = session;
            _stringResourceProvider = stringResourceProvider;
            _mediaCategoryRepository = mediaCategoryRepository;
            _mediaSettings = mediaSettings;
            _getMediaCategoriesByParent = getMediaCategoriesByParent;
            _mapper = mapper;
        }

        public async Task<ViewDataUploadFilesResult> AddFile(Stream stream, string fileName, string contentType,
            long contentLength,
            int? mediaCategoryId)
        {
            MediaFile mediaFile = await _fileService.AddFile(stream, fileName, contentType, contentLength,
                mediaCategoryId.HasValue ? await _session.GetAsync<MediaCategory>(mediaCategoryId) : null);
            return mediaFile.GetUploadFilesResult();
        }

        public async Task DeleteFile(int id)
        {
            await _fileService.DeleteFile(id);
        }

        public async Task<MediaFile> UpdateSEO(UpdateFileSEOModel model)
        {
            var file = await _session.GetAsync<MediaFile>(model.Id);
            _mapper.Map(model, file);
            await _fileService.SaveFile(file);
            return file;
        }

        public bool IsValidFileType(string fileName)
        {
            return _fileService.IsValidFileType(fileName);
        }

        public async Task<IPagedList<MediaFile>> GetFilesForFolder(MediaCategorySearchModel searchModel)
        {
            IQueryOver<MediaFile, MediaFile> query = _session.QueryOver<MediaFile>();
            query = searchModel.Id.HasValue
                ? query.Where(file => file.MediaCategory.Id == searchModel.Id)
                : query.Where(file => file.MediaCategory == null);
            if (!string.IsNullOrWhiteSpace(searchModel.SearchText))
            {
                query = query.Where(file =>
                    file.FileName.IsInsensitiveLike(searchModel.SearchText, MatchMode.Anywhere)
                    ||
                    file.Title.IsInsensitiveLike(searchModel.SearchText, MatchMode.Anywhere)
                    ||
                    file.Description.IsInsensitiveLike(searchModel.SearchText, MatchMode.Anywhere)
                );
            }

            query = query.OrderBy(searchModel.SortBy);

            return await query.PagedAsync(searchModel.Page, _mediaSettings.MediaPageSize);
        }

        public async Task<IList<ImageSortItem>> GetFilesToSort(MediaCategory category = null)
        {
            IQueryOver<MediaFile, MediaFile> query = _session.QueryOver<MediaFile>();
            query = category != null
                ? query.Where(file => file.MediaCategory.Id == category.Id)
                : query.Where(file => file.MediaCategory == null);
            query = query.OrderBy(x => x.DisplayOrder).Asc;

            ImageSortItem item = null;
            return (await query.SelectList(builder =>
                {
                    builder.Select(file => file.FileName).WithAlias(() => item.Name);
                    builder.Select(file => file.Id).WithAlias(() => item.Id);
                    builder.Select(file => file.DisplayOrder).WithAlias(() => item.Order);
                    builder.Select(file => file.FileExtension).WithAlias(() => item.FileExtension);
                    builder.Select(file => file.FileUrl).WithAlias(() => item.ImageUrl);
                    return builder;
                }).TransformUsing(Transformers.AliasToBean<ImageSortItem>())
                .Cacheable()
                .ListAsync<ImageSortItem>());
        }


        public async Task SetOrders(List<SortItem> items)
        {
            await _session.TransactAsync(async session =>
            {
                foreach (var item in items)
                {
                    var mediaFile = await session.GetAsync<MediaFile>(item.Id);
                    mediaFile.DisplayOrder = item.Order;
                    await session.UpdateAsync(mediaFile);
                }
            });
        }

        public async Task<IList<MediaCategory>> GetSubFolders(MediaCategorySearchModel searchModel)
        {
            IQueryOver<MediaCategory, MediaCategory> queryOver =
                _session.QueryOver<MediaCategory>().Where(x => !x.HideInAdminNav);
            queryOver = searchModel.Id.HasValue
                ? queryOver.Where(x => x.Parent.Id == searchModel.Id)
                : queryOver.Where(x => x.Parent == null);
            if (!string.IsNullOrWhiteSpace(searchModel.SearchText))
            {
                queryOver =
                    queryOver.Where(
                        category => category.Name.IsInsensitiveLike(searchModel.SearchText, MatchMode.Anywhere));
            }

            queryOver = queryOver.OrderBy(searchModel.SortBy);

            return await queryOver.Cacheable().ListAsync();
        }

        public async Task<UpdateFileSEOModel> GetEditModel(int id)
        {
            var file = await _session.GetAsync<MediaFile>(id);

            return _mapper.Map<UpdateFileSEOModel>(file);
        }


        public async Task<string> MoveFolders(IEnumerable<int> folderIds, int? parentId = null)
        {
            string message = string.Empty;
            if (folderIds != null)
            {
                await _session.TransactAsync(async session =>
                {
                    foreach (var item in folderIds)
                    {
                        var mediaFolder = await session.GetAsync<MediaCategory>(item);
                        if (parentId != null && mediaFolder.Id != parentId)
                        {
                            mediaFolder.Parent = _session.Get<MediaCategory>(parentId.Value);
                            await session.UpdateAsync(mediaFolder);
                        }
                        else if (parentId == null)
                        {
                            mediaFolder.Parent = null;
                            await session.UpdateAsync(mediaFolder);
                        }
                        else
                        {
                            message = await _stringResourceProvider.GetValue("Cannot move folder to the same folder");
                        }
                    }
                });
            }

            return message;
        }

        public async Task MoveFiles(IEnumerable<int> files, int? parentId = null)
        {
            if (files != null)
            {
                await _session.TransactAsync(async session =>
                {
                    foreach (var item in files)
                    {
                        var mediaFile = await session.GetAsync<MediaFile>(item);
                        mediaFile.MediaCategory = parentId.HasValue ? _session.Get<MediaCategory>(parentId) : null;
                        await session.UpdateAsync(mediaFile);
                    }
                });
            }
        }

        public async Task DeleteFoldersSoft(IEnumerable<int> folderIds)
        {
            if (folderIds != null)
            {
                var ids = folderIds.ToList();
                var folders = await _session.Query<MediaCategory>().Where(x => ids.Contains(x.Id)).ToListAsync();
                var foldersRecursive = await GetFoldersRecursive(folders);
                await _mediaCategoryRepository.TransactAsync(async repository =>
                {
                    foreach (var f in foldersRecursive)
                    {
                        var folder = await repository.Get(f.Id);
                        var files = folder.Files.ToList();
                        foreach (var file in files)
                            await _fileService.DeleteFileSoft(file.Id);

                        await repository.Delete(folder);
                    }
                });
            }
        }
        
        public async Task DeleteFoldersHard(IEnumerable<int> folderIds)
        {
            if (folderIds != null)
            {
                var ids = folderIds.ToList();
                var folders = await _session.Query<MediaCategory>().Where(x => ids.Contains(x.Id)).ToListAsync();
                var foldersRecursive = await GetFoldersRecursive(folders);
                await _mediaCategoryRepository.TransactAsync(async repository =>
                {
                    foreach (var f in foldersRecursive)
                    {
                        var folder = await repository.Get(f.Id);
                        var files = folder.Files.ToList();
                        foreach (var file in files)
                            await _fileService.DeleteFile(file.Id);

                        await _fileService.RemoveFolder(folder);
                        await repository.Delete(folder);
                    }
                });
            }
        }

        public async Task<MediaCategory> GetCategory(MediaCategorySearchModel searchModel)
        {
            return searchModel.Id.HasValue
                ? await _session.GetAsync<MediaCategory>(searchModel.Id.Value)
                : null;
        }

        public List<SelectListItem> GetSortByOptions(MediaCategorySearchModel searchModel)
        {
            return EnumHelper<MediaCategorySortMethod>.GetOptions();
        }

        public async Task<MediaFile> GetFile(int id)
        {
            return await _session.GetAsync<MediaFile>(id);
        }

        public async Task DeleteFilesSoft(IEnumerable<int> fileIds)
        {
            if (fileIds != null)
            {
                foreach (int file in fileIds)
                    await _fileService.DeleteFileSoft(file);
            }
        }

        public async Task DeleteFilesHard(IEnumerable<int> fileIds)
        {
            if (fileIds != null)
            {
                foreach (int file in fileIds)
                    await _fileService.DeleteFile(file);
            }
        }

        private async Task<IReadOnlyList<MediaCategory>> GetFoldersRecursive(IEnumerable<MediaCategory> categories)
        {
            var list = new List<MediaCategory>();
            foreach (MediaCategory category in categories)
            {
                list.AddRange(
                    await GetFoldersRecursive(await _getMediaCategoriesByParent.GetMediaCategories(category)));

                list.Add(category);
            }

            return list;
        }
    }
}