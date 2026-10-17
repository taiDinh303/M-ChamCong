import axiosClient from "./axiosClient";
import { collectPages } from "./contracts";

export const getAllPages = (path, params = {}) => collectPages((pageNumber, pageSize) =>
    axiosClient.get(path, { params: { ...params, pageNumber, pageSize } }));
