using AutoMapper;
using dai.core.DTO.Board;
using dai.core.DTO.Label;
using dai.core.DTO.Note;
using dai.core.DTO.NoteLabel;
using dai.core.DTO.Workspace;
using dai.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dai.core.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<NoteModel, NoteDTO>().ReverseMap();
            CreateMap<NoteModel, GetNoteDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

            CreateMap<LabelModel, LabelDTO>().ReverseMap();

            CreateMap<LabelModel, GetLabelDTO>()
            .ForMember(dest => dest.LabelId, opt => opt.MapFrom(src => src.Id));

            CreateMap<NoteLabelModel, NoteLabelDTO>().ReverseMap();

            //Board Mappings
            CreateMap<BoardModel, BoardDto>();
            CreateMap<CreateBoardDto, BoardModel>()
                .ForMember(dest => dest.Create_At, opt => opt.Ignore())
                .ForMember(dest => dest.Update_At, opt => opt.Ignore());
            CreateMap<UpdateBoardDto, BoardModel>()
                .ForMember(dest => dest.Update_At, opt => opt.Ignore());

            // Workspace mappings
            CreateMap<CreateWorkspaceDto, WorkspaceModel>()
                .ForMember(dest => dest.Create_At, opt => opt.Ignore()) 
                .ForMember(dest => dest.Update_At, opt => opt.Ignore()) 
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"));

            CreateMap<WorkspaceModel, WorkspaceDto>();
            CreateMap<UpdateWorkspaceDto, WorkspaceModel>()
                .ForMember(dest => dest.Update_At, opt => opt.Ignore());


        }
    }
}