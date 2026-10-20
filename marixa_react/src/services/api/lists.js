import axiosClient from "./axiosClient";
import { collectPages, peekPages } from "./contracts";
import { getAuth } from "../auth/auth";
import { responseCache, sessionScope, cacheKey } from "./responseCache.js";

export const getAllPages = (path, params = {}) => collectPages((pageNumber, pageSize) =>
    axiosClient.get(path, { params: { ...params, pageNumber, pageSize } }));

export const getCachedPages = (path, params = {}) => {
    const scope = sessionScope(getAuth());
    if (!scope) return null;
    return peekPages((pageNumber, pageSize) => responseCache.peek(cacheKey(axiosClient.getUri({
        url: path, params: { ...params, pageNumber, pageSize },
    })), scope));
};
