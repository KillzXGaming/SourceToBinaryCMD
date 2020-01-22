﻿using System;
using System.Collections.Generic;
using System.Text;
using Collada141;
using OpenTK;

namespace Source2Binary.Collada
{
    public class DaeUtility
    {
        public static controller FindControllerFromNode(node daeNode, library_controllers controllers)
        {
            string mesh_id = daeNode.instance_controller[0].url.Trim('#');
            return Array.Find(controllers.controller, x => x.id == mesh_id);
        }

        public static geometry FindGeoemertyFromController(controller controller, library_geometries geometries)
        {
            skin skin = controller.Item as skin;
            string mesh_id = skin.source1.Trim('#');
            return Array.Find(geometries.geometry, x => x.id == mesh_id);
        }

        public static geometry FindGeoemertyFromNode(node daeNode, library_geometries geometries)
        {
            string mesh_id = daeNode.instance_geometry[0].url.Trim('#');
            return Array.Find(geometries.geometry, x => x.id == mesh_id);
        }

        public static source FindSourceFromInput(InputLocalOffset input, source[] sources)
        {
            string inputSource = input.source.Trim('#');
            return Array.Find(sources, x => x.id == inputSource);
        }

        public static source FindSourceFromInput(InputLocal input, source[] sources)
        {
            string inputSource = input.source.Trim('#');
            return Array.Find(sources, x => x.id == inputSource);
        }

        public static Matrix4 GetMatrix(object[] items)
        {
            Matrix4 transform = Matrix4.Identity;
            for (int i = 0; i < items?.Length; i++)
            {
                if (items[i] is matrix)
                    transform = FloatToMatrix(((matrix)items[i]).Values);
            }
            return transform;
        }

        public static Matrix4 FloatToMatrix(double[] values)
        {
            if (values?.Length != 16)
                return Matrix4.Identity;

            var mat = new Matrix4(
                (float)values[0], (float)values[4], (float)values[8], (float)values[12],
                (float)values[1], (float)values[5], (float)values[9], (float)values[13],
                (float)values[2], (float)values[6], (float)values[10], (float)values[14],
                (float)values[3], (float)values[7], (float)values[11], (float)values[15]);

            return mat;
        }

        public static int FindMaterialIndex(library_materials materials, string name)
        {
            if (materials != null)
            {
                for (int i = 0; i < materials.material.Length; i++)
                {
                    if (materials.material[i].name == name)
                        return i;
                }
            }
            return -1;
        }
    }
}
