using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.Entities.Documents.Media;
using MrCMS.Models;
using MrCMS.Web.Admin.Models;
using X.PagedList;

namespace MrCMS.Web.Admin.Services
{
    public interface IFileAdminService
    {
        Task<ViewDataUploadFilesResult> AddFile(Stream stream, string fileName, string contentType, long contentLength,
            int? mediaCategoryId);

        Task DeleteFile(int id);
        Task<MediaFile> UpdateSEO(UpdateFileSEOModel model);
        bool IsValidFileType(string fileName);
        Task<IPagedList<MediaFile>> GetFilesForFolder(MediaCategorySearchModel searchModel);
        Task<IList<ImageSortItem>> GetFilesToSort(MediaCategory category = null);
        Task SetOrders(List<SortItem> items);
        Task<IList<MediaCategory>> GetSubFolders(MediaCategorySearchModel searchModel);
        Task<UpdateFileSEOModel> GetEditModel(int id);

        Task<string> MoveFolders(IEnumerable<int> folderIds, int? parentId = null);
        Task MoveFiles(IEnumerable<int> fileIds, int? parentId = null);
        Task DeleteFilesSoft(IEnumerable<int> fileIds);
        Task DeleteFilesHard(IEnumerable<int> fileIds);
        Task DeleteFoldersSoft(IEnumerable<int> folderIds);
        Task DeleteFoldersHard(IEnumerable<int> folderIds);
        Task<MediaCategory> GetCategory(MediaCategorySearchModel searchModel);
        List<SelectListItem> GetSortByOptions(MediaCategorySearchModel searchModel);
        Task<MediaFile> GetFile(int id);
    }
}